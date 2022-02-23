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

            if (activity.ActivityType == Dynamics365Constants.ACTIVITY_PHONECALL)
            {
                MapPhoneCallProperties(dynamicsId, entity, activity);
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
    }
}