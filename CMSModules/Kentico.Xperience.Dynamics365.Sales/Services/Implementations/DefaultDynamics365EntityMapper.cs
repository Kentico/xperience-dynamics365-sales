using CMS;
using CMS.Activities;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models.Activities;
using Kentico.Xperience.Dynamics365.Sales.Models.Mapping;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

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
        private readonly IProgressiveCache progressiveCache;


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
            IEventLogService eventLogService,
            IProgressiveCache progressiveCache)
        {
            this.settingsService = settingsService;
            this.dynamics365Client = dynamics365Client;
            this.eventLogService = eventLogService;
            this.progressiveCache = progressiveCache;
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


        public Dictionary<string, string> MapChangedColumns(MappingModel mapping, BaseInfo xperienceObject, JObject entity)
        {
            var result = new Dictionary<string, string>();
            foreach (var item in mapping.Items.Where(i => !String.IsNullOrEmpty(i.Dynamics365Field)))
            {
                if (!xperienceObject.ContainsColumn(item.XperienceFieldName))
                {
                    continue;
                }

                var baseInfoValue = GetXperienceValue(item, xperienceObject);
                var dynamicsValue = entity.Value<string>(item.Dynamics365Field);
                if (String.IsNullOrEmpty(dynamicsValue) || ValidationHelper.GetString(baseInfoValue, String.Empty) == dynamicsValue)
                {
                    continue;
                }

                result.Add(item.XperienceFieldName, dynamicsValue);
            }

            return result;
        }


        public JObject MapEntity(string entityName, MappingModel mapping, BaseInfo sourceObject)
        {
            var entity = new JObject();
            foreach (var item in mapping.Items)
            {
                var baseInfoValue = GetXperienceValue(item, sourceObject);
                if (baseInfoValue == null)
                {
                    continue;
                }

                entity.Add(item.Dynamics365Field, JToken.FromObject(baseInfoValue));
            }

            DecodeValues(entity);

            return entity;
        }


        public JObject MapPartialEntity(string entityName, MappingModel mapping, string dynamicsId, BaseInfo sourceObject)
        {
            var fullEntity = MapEntity(entityName, mapping, sourceObject);

            try
            {
                // Get contact info from Dynamics
                var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_PATCH_GETSINGLE, entityName, dynamicsId);
                var response = dynamics365Client.SendRequest(endpoint, HttpMethod.Get);
                var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    eventLogService.LogError(nameof(DefaultDynamics365EntityMapper), nameof(MapPartialEntity), $"Error while retrieving existing Dynamics 365 Entity: {responseContent}");
                    return null;
                }

                var dynamicsObject = JObject.Parse(responseContent);
                foreach (var item in mapping.Items)
                {
                    var baseInfoValue = GetXperienceValue(item, sourceObject);
                    var dynamicsValue = dynamicsObject.Value<string>(item.Dynamics365Field);
                    if (baseInfoValue == null || ValidationHelper.GetString(baseInfoValue, String.Empty) == dynamicsValue)
                    {
                        // Column doesn't exist in Xperience, or the value matches Dynamics
                        fullEntity.Remove(item.Dynamics365Field);
                    }
                }

                return fullEntity;
            }
            catch (Exception ex)
            {
                eventLogService.LogError(nameof(DefaultDynamics365EntityMapper), nameof(MapPartialEntity), ex.Message);
                return null;
            }
        }


        /// <summary>
        /// Calls <see cref="HTMLHelper.HTMLDecode"/> on all string values in the <paramref name="entity"/>.
        /// </summary>
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


        /// <summary>
        /// Gets the value of the <see cref="MappingItem.XperienceFieldName"/> from the <paramref name="baseInfo"/>.
        /// </summary>
        /// <param name="mapping">The mapping containing the Xperience field name to retrieve the value of.</param>
        /// <param name="baseInfo">The Xperience object to retrieve the value from.</param>
        /// <returns>The Xperience object's field value.</returns>
        private object GetXperienceValue(MappingItem mapping, BaseInfo baseInfo)
        {
            var baseInfoValue = baseInfo.GetValue(mapping.XperienceFieldName);

            if (mapping.DynamicsAttributeType == EntityAttributeType.DATETIME)
            {
                // Convert DateTime to either Date and Time or only Date
                return GetValidDateTime(baseInfoValue, mapping);
            }

            return baseInfoValue;
        }


        /// <summary>
        /// Converts an Xperience object's value into the proper format for upserting into Dynamics 365.
        /// </summary>
        /// <param name="xperienceValue">The Xperience object's field value.</param>
        /// <param name="mapping">The mapping that was used to retrieve the <paramref name="xperienceValue"/>.</param>
        /// <returns>A valid Edm.Date value in string format.</returns>
        private string GetValidDateTime(object xperienceValue, MappingItem mapping)
        {
            var retVal = ValidationHelper.GetDateTime(xperienceValue, DateTime.MinValue);
            if (retVal == DateTime.MinValue)
            {
                return null;
            }

            if (mapping.DynamicsAttributeFormat == "DateOnly")
            {
                return retVal.ToString("yyyy-MM-dd");
            }
            else if (mapping.DynamicsAttributeFormat == "DateAndTime")
            {
                return retVal.ToString();
            }

            return null;
        }


        /// <summary>
        /// Maps Entity properties which all activities should contain.
        /// </summary>
        /// <param name="dynamicsId">The internal Dynamics 365 contact ID associated with the activity.</param>
        /// <param name="entity">The Entity to map.</param>
        /// <param name="relatedData">An object containing the required data for the activity, such as <see cref="ActivityInfo"/>.</param>
        private void MapCommonActivityProperties(string dynamicsId, JObject entity, object relatedData)
        {
            if (relatedData is ActivityInfo)
            {
                entity.Add("scheduledstart", (relatedData as ActivityInfo).ActivityCreated);
            }

            entity.Add("isregularactivity", true);
            entity.Add($"regardingobjectid_{Dynamics365Constants.ENTITY_CONTACT}@odata.bind", $"/{Dynamics365Constants.ENTITY_CONTACT}s({dynamicsId})");

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
                parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.RequiredAttendee), new JProperty($"partyid_{Dynamics365Constants.ENTITY_USER}@odata.bind", appointmentModel.RequiredAttendee)));
            }

            if (!String.IsNullOrEmpty(appointmentModel.OptionalAttendee))
            {
                parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.OptionalAttendee), new JProperty($"partyid_{Dynamics365Constants.ENTITY_USER}@odata.bind", appointmentModel.OptionalAttendee)));
            }

            if (!String.IsNullOrEmpty(DefaultOwner) && DefaultOwner.StartsWith($"/{Dynamics365Constants.ENTITY_USER}"))
            {
                parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.Organizer), new JProperty($"partyid_{Dynamics365Constants.ENTITY_USER}@odata.bind", DefaultOwner)));
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

            parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.ToRecipient), new JProperty($"partyid_{Dynamics365Constants.ENTITY_CONTACT}@odata.bind", $"/{Dynamics365Constants.ENTITY_CONTACT}s({phoneCallModel.To})")));

            if (!String.IsNullOrEmpty(phoneCallModel.From))
            {
                parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.Sender), new JProperty($"partyid_{Dynamics365Constants.ENTITY_USER}@odata.bind", $"/{Dynamics365Constants.ENTITY_USER}s({phoneCallModel.From})")));

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
                    parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.ToRecipient), new JProperty($"partyid_{Dynamics365Constants.ENTITY_USER}@odata.bind", $"/{Dynamics365Constants.ENTITY_USER}s({emailModel.To})")));
                }
                else
                {
                    parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.ToRecipient), new JProperty($"partyid_{Dynamics365Constants.ENTITY_CONTACT}@odata.bind", $"/{Dynamics365Constants.ENTITY_CONTACT}s({emailModel.To})")));
                }
            }
            else
            {
                // Email was sent to contact before they were linked to a Dynamics contact
                parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.ToRecipient), new JProperty($"partyid_{Dynamics365Constants.ENTITY_CONTACT}@odata.bind", $"/{Dynamics365Constants.ENTITY_CONTACT}s({dynamicsId})")));
            }

            if (!String.IsNullOrEmpty(emailModel.From))
            {
                parties.Add(new JObject(new JProperty("participationtypemask", ParticipationTypeMaskEnum.Sender), new JProperty($"partyid_{Dynamics365Constants.ENTITY_USER}@odata.bind", $"/{Dynamics365Constants.ENTITY_USER}s({emailModel.From})")));

            }

            if (parties.Count > 0)
            {
                entity.Add("email_activity_parties", parties);
            }
        }
    }
}