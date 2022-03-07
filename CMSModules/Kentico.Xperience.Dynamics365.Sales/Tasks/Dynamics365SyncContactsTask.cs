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

            // Get previously synced contacts that were modified since task last run
            var contactsToSync = contactSyncService.GetSynchronizedContacts().Where(contact => contact.ContactLastModified > task.TaskLastRunTime).ToList();

            // Add contacts that meet scoring requirements, but not synced
            contactsToSync.AddRange(contactSyncService.GetContactsWithScore());

            var result = contactSyncService.SynchronizeContacts(contactsToSync);
            if (result.SynchronizationErrors.Count > 0)
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