using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.Scheduler;

using Kentico.Xperience.Dynamics365.Sales.Services;

using System;

namespace Kentico.Xperience.Dynamics365.Sales.Tasks
{
    public class Dynamics365SyncContactsTask : ITask
    {
        public string Execute(TaskInfo task)
        {
            var minimumScore = SettingsKeyInfoProvider.GetIntValue("Dynamics365MinScore");
            if (minimumScore <= 0)
            {
                return String.Empty;
            }

            var contactsWithScore = ContactInfo.Provider.Get()
                .WhereIn(
                    nameof(ContactInfo.ContactID),
                    ScoreContactRuleInfoProvider.GetContactsWithScore(minimumScore).AsSingleColumn(nameof(ContactInfo.ContactID))
                )
                .TypedResult
                .ToContactList();

            Service.Resolve<IDynamics365ContactSync>().SynchronizeContacts(contactsWithScore);

            return String.Empty;
        }
    }
}