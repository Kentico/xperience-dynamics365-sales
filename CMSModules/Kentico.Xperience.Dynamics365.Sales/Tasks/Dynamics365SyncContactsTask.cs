using CMS.Core;
using CMS.Scheduler;

using Kentico.Xperience.Dynamics365.Sales.Services;

using System;
using System.Linq;

namespace Kentico.Xperience.Dynamics365.Sales.Tasks
{
    /// <summary>
    /// Upserts contacts who meet the scoring requirement to Dynamics 365.
    /// </summary>
    public class Dynamics365SyncContactsTask : ITask
    {
        public string Execute(TaskInfo task)
        {
            var contactSyncService = Service.Resolve<IDynamics365ContactSync>();
            if (!contactSyncService.SynchronizationEnabled())
            {
                return "Contact synchronization is disabled.";
            }

            // Merge contacts with score and contacts with linked ID, as they may have been synced by automation step without
            // meeting score requirements
            var contactsWithScore = contactSyncService.GetContactsWithScore().ToList();
            var contactsWithLink = contactSyncService.GetSynchronizedContacts().ToList();
            var contactsToSync = contactsWithLink.Union(contactsWithScore);

            var result = contactSyncService.SynchronizeContacts(contactsToSync);

            if (result.HasErrors)
            {
                var message = $"The following errors occurred during synchronization:<br/><br/>{String.Join("<br/>", result.SynchronizationErrors)}"
                    + $"<br/><br/>As a result, the following contacts were not synchronized:<br/><br/>{String.Join("<br/>", result.UnsynchronizedObjectIdentifiers)}";
                Service.Resolve<IEventLogService>().LogError(nameof(Dynamics365SyncContactsTask), nameof(Execute), message);

                return "Synchronization errors occured, please check the Event Log.";
            }

            return $"{result.SynchronizedObjectCount} contacts synchronized.";
        }
    }
}