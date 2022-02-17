using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.Scheduler;

using Kentico.Xperience.Dynamics365.Sales.Services;

using System;

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

            var minimumScore = SettingsKeyInfoProvider.GetIntValue(Dynamics365Constants.SETTINGS_MINSCORE);
            var contactsWithScore = ContactInfo.Provider.Get()
                .WhereIn(
                    nameof(ContactInfo.ContactID),
                    ScoreContactRuleInfoProvider.GetContactsWithScore(minimumScore).AsSingleColumn(nameof(ContactInfo.ContactID))
                )
                .TypedResult
                .ToContactList();

            var result = contactSyncService.SynchronizeContacts(contactsWithScore).ConfigureAwait(false).GetAwaiter().GetResult();

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