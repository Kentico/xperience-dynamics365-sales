using CMS.Activities;
using CMS.Core;
using CMS.Scheduler;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Services;

using System;
using System.Linq;

namespace Kentico.Xperience.Dynamics365.Sales.Tasks
{
    /// <summary>
    /// Creates activities in Dynamics 365 that were performed since last run.
    /// </summary>
    public class Dynamics365SyncActivitiesTask : ITask
    {
        private IEventLogService eventLogService;
        private IDynamics365ActivitySync activitySync;


        public string Execute(TaskInfo task)
        {
            activitySync = Service.Resolve<IDynamics365ActivitySync>();
            eventLogService = Service.Resolve<IEventLogService>();

            if (!activitySync.SynchronizationEnabled())
            {
                return "Activity synchronization is disabled.";
            }

            var hasErrors = false;
            var totalSynchronized = 0;
            var synchronizedContacts = Service.Resolve<IDynamics365ContactSync>().GetSynchronizedContacts();
            foreach(var contact in synchronizedContacts)
            {
                var dynamicsId = contact.GetStringValue(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, String.Empty);
                var activitiesSinceLastRun = ActivityInfo.Provider.Get()
                    .WhereEquals(nameof(ActivityInfo.ActivityContactID), contact.ContactID)
                    //.WhereGreaterOrEquals(nameof(ActivityInfo.ActivityCreated), task.TaskLastRunTime)
                    .TypedResult
                    .ToList();

                var result = Service.Resolve<IDynamics365ActivitySync>().SynchronizeActivities(dynamicsId, activitiesSinceLastRun).ConfigureAwait(false).GetAwaiter().GetResult();

                totalSynchronized += result.SynchronizedObjectCount;
                if (result.HasErrors)
                {
                    hasErrors = true;
                    var message = $"The following errors occurred during synchronization of contact '{contact.ContactDescriptiveName}' activities:<br/><br/>{String.Join("<br/>", result.SynchronizationErrors)}"
                        + $"<br/><br/>As a result, the following activities were not synchronized:<br/><br/>{String.Join("<br/>", result.UnsynchronizedObjectIdentifiers)}";
                    eventLogService.LogError(nameof(Dynamics365SyncActivitiesTask), nameof(Execute), message);
                }
            }

            if (hasErrors)
            {
                return "Synchronization errors occured, please check the Event Log.";
            }

            return $"{totalSynchronized} activities synchronized.";
        }
    }
}