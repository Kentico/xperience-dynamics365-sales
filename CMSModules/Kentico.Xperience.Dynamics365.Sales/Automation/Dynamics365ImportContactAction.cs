using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models.Mapping;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json;

using System;
using System.Net.Http;

namespace Kentico.Xperience.Dynamics365.Sales.Automation
{
    /// <summary>
    /// A custom Marketing automation action which synchronizes a contact with Dynamics 365 regardless of
    /// the contact's score.
    /// </summary>
    public class Dynamics365ImportContactAction : AutomationAction
    {
        public override void Execute()
        {
            var mappingDefinition = Service.Resolve<ISettingsService>()[Dynamics365Constants.SETTING_FIELDMAPPING];
            if (String.IsNullOrEmpty(mappingDefinition))
            {
                throw new InvalidOperationException("Unable to load contact field mapping. Please check the settings.");
            }

            var mapping = JsonConvert.DeserializeObject<MappingModel>(mappingDefinition);
            var contact = InfoObject as ContactInfo;
            var dynamicsId = contact.GetStringValue(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, String.Empty);
            var contactSyncService = Service.Resolve<IDynamics365ContactSync>();
            var entityMapper = Service.Resolve<IDynamics365EntityMapper>();
            HttpResponseMessage response = null;
            if (String.IsNullOrEmpty(dynamicsId))
            {
                var fullEntity = entityMapper.MapEntity(Dynamics365Constants.ENTITY_CONTACT, mapping, contact);
                response = contactSyncService.CreateContact(contact, fullEntity);
            }
            else
            {
                var partialEntity = entityMapper.MapPartialEntity(Dynamics365Constants.ENTITY_CONTACT, mapping, dynamicsId, contact);
                response = contactSyncService.UpdateContact(dynamicsId, partialEntity);
            }

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                Service.Resolve<IEventLogService>().LogError(nameof(Dynamics365ImportContactAction), nameof(Execute), $"Encountered an error while synchronizing contact '{contact.ContactDescriptiveName}': {responseContent}");
            }
        }
    }
}