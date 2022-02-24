using CMS;
using CMS.Activities;
using CMS.Core;
using CMS.DataEngine;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models.Activities;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

[assembly: RegisterImplementation(typeof(IDynamics365EntityMapper), typeof(DefaultDynamics365EntityMapper), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    public class DefaultDynamics365EntityMapper : IDynamics365EntityMapper
    {
        private readonly ISettingsService settingsService;


        public DefaultDynamics365EntityMapper(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
        }


        public string MapActivityType(string activityType)
        {
            return activityType;
        }


        public JObject MapEntity(string entityName, string dynamicsId, ActivityInfo activity)
        {
            var entity = new JObject();

            entity.Add("actualstart", activity.ActivityCreated);
            entity.Add("isregularactivity", true);
            entity.Add("regardingobjectid_contact@odata.bind", $"/contacts({dynamicsId})");

            // Set activity owner
            var defaultOwner = settingsService[Dynamics365Constants.SETTING_DEFAULTOWNER];
            if (!String.IsNullOrEmpty(defaultOwner))
            {
                entity.Add("ownerid@odata.bind", defaultOwner);
            }

            switch (entityName)
            {
                case Dynamics365Constants.ACTIVITY_PHONECALL:
                    MapPhoneCallProperties(dynamicsId, entity, activity);
                    break;
                case Dynamics365Constants.ACTIVITY_EMAIL:
                    MapEmailProperties(dynamicsId, entity, activity);
                    break;
            }

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

            return entity;
        }


        private void MapPhoneCallProperties(string dynamicsId, JObject entity, ActivityInfo activity)
        {
            var phoneCallModel = JsonConvert.DeserializeObject<Dynamics365PhoneCallModel>(activity.ActivityValue);
            entity.Add("subject", phoneCallModel.Subject);
            entity.Add("phonenumber", phoneCallModel.PhoneNumber);
            entity.Add("description", phoneCallModel.Description);

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


        private void MapEmailProperties(string dynamicsId, JObject entity, ActivityInfo activity)
        {
            var emailModel = JsonConvert.DeserializeObject<Dynamics365EmailModel>(activity.ActivityValue);
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