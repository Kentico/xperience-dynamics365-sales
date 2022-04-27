using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;

using Kentico.Xperience.Dynamics365.Sales.Services;

using System;

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
            var contact = InfoObject as ContactInfo;
            var contactSyncService = Service.Resolve<IDynamics365ContactSync>();
            var result = contactSyncService.SynchronizeContacts(new ContactInfo[] { contact });
            if (result.SynchronizationErrors.Count > 0)
            {
                Service.Resolve<IEventLogService>().LogError(nameof(Dynamics365ImportContactAction), nameof(Execute),
                    $"Encountered errors while synchronizing contact '{contact.ContactDescriptiveName}':<br/><br/>{String.Join("<br/><br/>", result.SynchronizationErrors)}");
            }
        }
    }
}