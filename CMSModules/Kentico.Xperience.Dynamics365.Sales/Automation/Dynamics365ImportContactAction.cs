using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Services;

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
            var eventLogService = Service.Resolve<IEventLogService>();
            var mappingDefinition = Service.Resolve<ISettingsService>()[Dynamics365Constants.SETTING_FIELDMAPPING];
            if (String.IsNullOrEmpty(mappingDefinition))
            {
                eventLogService.LogError(nameof(Dynamics365ImportContactAction), nameof(Execute), "Unable to load contact field mapping. Please check the settings.");
                return;
            }

            var contact = InfoObject as ContactInfo;
            var dynamicsId = contact.GetStringValue(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, String.Empty);
            var contactSyncService = Service.Resolve<IDynamics365ContactSync>();
            var entityMapper = Service.Resolve<IDynamics365EntityMapper>();
            HttpResponseMessage response = null;
            if (String.IsNullOrEmpty(dynamicsId))
            {
                var fullEntity = entityMapper.MapEntity("contact", mappingDefinition, contact);
                response = contactSyncService.CreateContact(contact, fullEntity).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                var partialEntity = entityMapper.MapPartialEntity("contact", mappingDefinition, dynamicsId, contact).ConfigureAwait(false).GetAwaiter().GetResult();
                response = contactSyncService.UpdateContact(dynamicsId, partialEntity).ConfigureAwait(false).GetAwaiter().GetResult();
            }

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                eventLogService.LogError(nameof(Dynamics365ImportContactAction), nameof(Execute), $"Encountered an error while synchronizing contact '{contact.ContactDescriptiveName}': {responseContent}");
            }
        }
    }
}