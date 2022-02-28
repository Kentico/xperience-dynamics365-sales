using CMS;
using CMS.Activities;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models.Activities;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

[assembly: RegisterImplementation(typeof(IDynamics365EntityMapper), typeof(DefaultDynamics365EntityMapper), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    /// <summary>
    /// The default implementation of <see cref="IDynamics365EntityMapper"/>.
    /// </summary>
    public class DefaultDynamics365EntityMapper : IDynamics365EntityMapper
    {
        private readonly ISettingsService settingsService;
        private readonly IDynamics365Client dynamics365Client;
        private readonly IEventLogService eventLogService;


        private string DefaultOwner
        {
            get
            {
                return settingsService[Dynamics365Constants.SETTING_DEFAULTOWNER];
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDynamics365EntityMapper"/> class.
        /// </summary>
        public DefaultDynamics365EntityMapper(
            ISettingsService settingsService,
            IDynamics365Client dynamics365Client,
            IEventLogService eventLogService)
        {
            this.settingsService = settingsService;
            this.dynamics365Client = dynamics365Client;
            this.eventLogService = eventLogService;
        }


        public string MapActivityType(string activityType)
        {
            return activityType;
        }


        public JObject MapActivity(string entityName, string dynamicsId, object relatedData)
        {
            var entity = new JObject();
            MapCommonActivityProperties(dynamicsId, entity, relatedData);

            switch (entityName)
            {
                case Dynamics365Constants.ACTIVITY_PHONECALL:
                    MapPhoneCallProperties(dynamicsId, entity, relatedData);
                    break;
                case Dynamics365Constants.ACTIVITY_EMAIL:
                    MapEmailProperties(dynamicsId, entity, relatedData);
                    break;
                case Dynamics365Constants.ACTIVITY_APPOINTMENT:
                    MapAppointmentProperties(dynamicsId, entity, relatedData);
                    break;
                case Dynamics365Constants.ACTIVITY_TASK:
                    MapTaskProperties(dynamicsId, entity, relatedData);
                    break;
            }

            DecodeValues(entity);

            return entity;
        }


        public JObject MapEntity(string entityName, string mapping, BaseInfo sourceObject)
        {
            var entity = JObject.Parse(mapping);
            var propertiesToRemove = new List<string>();
            foreach (var entityProperty in entity.Properties())
            {
                var baseInfoPropertyName = entityProperty.Value.Value<string>();
                var baseInfoValue = sourceObject.GetValue(baseInfoPropertyName);
                if (baseInfoValue == null)
                {
                    propertiesToRemove.Add(entityProperty.Name);
                    continue;
                }

                entityProperty.Value = JToken.FromObject(baseInfoValue);
            }

            foreach (var propertyName in propertiesToRemove)
            {
                entity.Remove(propertyName);
            }

            DecodeValues(entity);

            return entity;
        }


        public async Task<JObject> MapPartialEntity(string entityName, string mapping, string dynamicsId, BaseInfo sourceObject)
        {
            var mappingDefinition = JObject.Parse(mapping);
            var fullEntity = MapEntity(entityName, mapping, sourceObject);
            var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_PATCH_GETSINGLE, entityName, dynamicsId);
            var response = await dynamics365Client.SendRequest(endpoint, HttpMethod.Get).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                eventLogService.LogError(nameof(DefaultDynamics365EntityMapper), nameof(MapPartialEntity), $"Error while retrieving existing Dynamics 365 Entity: {responseContent}");
                return null;
            }

            var dynamicsObject = JObject.Parse(responseContent);
            var propertiesToRemove = new List<string>();
            foreach(var property in fullEntity.Properties())
            {
                var xperienceColumnName = mappingDefinition.Value<string>(property.Name);
                var xperienceValue = sourceObject.GetStringValue(xperienceColumnName, String.Empty);
                if (String.IsNullOrEmpty(xperienceValue) || xperienceValue == dynamicsObject.Value<string>(property.Name))
                {
                    // Column doesn't exist in Xperience, or the value matches Dynamics
                    propertiesToRemove.Add(property.Name);
                }
            }

            foreach (var propertyName in propertiesToRemove)
            {
                fullEntity.Remove(propertyName);
            }

            return fullEntity;
        }


        private void DecodeValues(JObject entity)
        {
            foreach(var property in entity.Properties())
            {
                if (property.Value.Type == JTokenType.String)
                {
                    property.Value = JToken.FromObject(HTMLHelper.HTMLDecode(property.Value.Value<string>()));
                }
            }
        }


        private void MapCommonActivityProperties(string dynamicsId, JObject entity, object relatedData)
        {
            if (relatedData is ActivityInfo)
            {
                entity.Add("actualstart", (relatedData as ActivityInfo).ActivityCreated);
            }

            entity.Add("isregularactivity", true);
            entity.Add("regardingobjectid_contact@odata.bind", $"/contacts({dynamicsId})");

            if (!String.IsNullOrEmpty(DefaultOwner))
            {
                entity.Add("ownerid@odata.bind", DefaultOwner);
            }
        }


        private void MapAppointmentProperties(string dynamicsId, JObject entity, object relatedData)
        {
            if (!(relatedData is Dynamics365AppointmentModel))
            {
                throw new InvalidOperationException($"{nameof(Dynamics365AppointmentModel)} is required to map the appointment activity.");
            }

            var appointmentModel = relatedData as Dynamics365AppointmentModel;
            entity.Add("subject", appointmentModel.Subject);
            entity.Add("isalldayevent", appointmentModel.IsAllDay);
            entity.Add("scheduleddurationminutes", appointmentModel.IsAllDay ? 1440 : ValidationHelper.GetInteger(appointmentModel.EndTime.Subtract(appointmentModel.StartTime).TotalMinutes, 0));

            if (!String.IsNullOrEmpty(appointmentModel.Description))
            {
                entity.Add("description", appointmentModel.Description);
            }

            var parties = new JArray();
            if (!String.IsNullOrEmpty(appointmentModel.RequiredAttendee))
            {
                parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.RequiredAttendee), new JProperty("partyid_systemuser@odata.bind", appointmentModel.RequiredAttendee)));
            }

            if (!String.IsNullOrEmpty(appointmentModel.OptionalAttendee))
            {
                parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.OptionalAttendee), new JProperty("partyid_systemuser@odata.bind", appointmentModel.OptionalAttendee)));
            }

            if (!String.IsNullOrEmpty(DefaultOwner) && DefaultOwner.StartsWith("/systemuser"))
            {
                parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.Organizer), new JProperty("partyid_systemuser@odata.bind", DefaultOwner)));
            }

            if (parties.Count > 0)
            {
                entity.Add("appointment_activity_parties", parties);
            }

            if (!String.IsNullOrEmpty(appointmentModel.Location))
            {
                entity.Add("location", appointmentModel.Location);
            }

            if (appointmentModel.StartTime > DateTime.MinValue)
            {
                entity.Add("scheduledstart", appointmentModel.StartTime);
            }

            if (appointmentModel.EndTime > DateTime.MinValue)
            {
                entity.Add("scheduledend", appointmentModel.EndTime);
            }
            else if (appointmentModel.IsAllDay)
            {
                entity.Add("scheduledend", appointmentModel.StartTime.AddDays(1));
            }
        }


        private void MapTaskProperties(string dynamicsId, JObject entity, object relatedData)
        {
            if (!(relatedData is Dynamics365TaskModel))
            {
                throw new InvalidOperationException($"{nameof(Dynamics365TaskModel)} is required to map the task activity.");
            }

            var taskModel = relatedData as Dynamics365TaskModel;
            entity.Add("subject",  taskModel.Subject);

            if (!String.IsNullOrEmpty(taskModel.Description))
            {
                entity.Add("description", taskModel.Description);
            }

            if (taskModel.DueDate != DateTime.MinValue)
            {
                entity.Add("scheduledend", taskModel.DueDate);
            }

            if (taskModel.DurationMinutes > 0)
            {
                entity.Add("actualdurationminutes", taskModel.DurationMinutes);
            }

            if (taskModel.Priority > -1)
            {
                entity.Add("prioritycode", taskModel.Priority);
            }
        }


        private void MapPhoneCallProperties(string dynamicsId, JObject entity, object relatedData)
        {
            if (!(relatedData is ActivityInfo))
            {
                throw new InvalidOperationException($"{nameof(ActivityInfo)} is required to map the phone call activity.");
            }

            var phoneCallModel = JsonConvert.DeserializeObject<Dynamics365PhoneCallModel>((relatedData as ActivityInfo).ActivityValue);
            entity.Add("subject", phoneCallModel.Subject);
            entity.Add("phonenumber", phoneCallModel.PhoneNumber);
            entity.Add("description", phoneCallModel.Description);

            var callStarted = (relatedData as ActivityInfo).ActivityCreated;
            entity.Add("actualend", callStarted.AddMinutes(phoneCallModel.Duration));
            entity.Add("scheduledend", callStarted.AddMinutes(phoneCallModel.Duration));
            entity.Add("actualdurationminutes", phoneCallModel.Duration);

            var parties = new JArray();
            if (String.IsNullOrEmpty(phoneCallModel.To))
            {
                phoneCallModel.To = dynamicsId;
            }

            parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.ToRecipient), new JProperty("partyid_contact@odata.bind", $"/contacts({phoneCallModel.To})")));

            if (!String.IsNullOrEmpty(phoneCallModel.From))
            {
                parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.Sender), new JProperty("partyid_contact@odata.bind", $"/systemusers({phoneCallModel.From})")));

            }

            entity.Add("phonecall_activity_parties", parties);
        }


        private void MapEmailProperties(string dynamicsId, JObject entity, object relatedData)
        {
            if (!(relatedData is ActivityInfo))
            {
                throw new InvalidOperationException($"{nameof(ActivityInfo)} is required to map the email activity.");
            }

            var emailModel = JsonConvert.DeserializeObject<Dynamics365EmailModel>((relatedData as ActivityInfo).ActivityValue);
            entity.Add("subject", emailModel.Subject);
            entity.Add("description", emailModel.Body);

            var parties = new JArray();
            if (!String.IsNullOrEmpty(emailModel.To))
            {
                if (emailModel.SentToUser)
                {
                    parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.ToRecipient), new JProperty("partyid_systemuser@odata.bind", $"/systemusers({emailModel.To})")));
                }
                else
                {
                    parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.ToRecipient), new JProperty("partyid_contact@odata.bind", $"/contacts({emailModel.To})")));
                }
            }

            if (!String.IsNullOrEmpty(emailModel.From))
            {
                parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.Sender), new JProperty("partyid_systemuser@odata.bind", $"/systemusers({emailModel.From})")));

            }

            if (parties.Count > 0)
            {
                entity.Add("email_activity_parties", parties);
            }
        }
    }
}