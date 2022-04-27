using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models.Activities;
using Kentico.Xperience.Dynamics365.Sales.Services;

using System;

namespace Kentico.Xperience.Dynamics365.Sales.Automation
{
    /// <summary>
    /// A custom Marketing automation action which creates a new task in Dynamics 365.
    /// </summary>
    public class Dynamics365TaskAction : AutomationAction
    {
        public override void Execute()
        {
            var subject = GetResolvedParameter("Subject", string.Empty);
            if (String.IsNullOrEmpty(subject))
            {
                throw new InvalidOperationException("The required properties are not set for the automation step.");
            }

            var contact = InfoObject as ContactInfo;
            var dynamicsId = contact.GetStringValue(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, string.Empty);
            if (string.IsNullOrEmpty(dynamicsId))
            {
                return;
            }

            var taskModel = new Dynamics365TaskModel
            {
                Subject = subject,
                Description = GetResolvedParameter("Description", string.Empty),
                DueDate = GetResolvedParameter("DueDate", DateTime.MinValue),
                DurationMinutes = GetResolvedParameter("DurationMinutes", 0),
                Priority = GetResolvedParameter("Priority", -1),
            };

            var entity = Service.Resolve<IDynamics365EntityMapper>().MapActivity(Dynamics365Constants.ACTIVITY_TASK, dynamicsId, taskModel);
            var response = Service.Resolve<IDynamics365ActivitySync>().CreateActivity(entity, Dynamics365Constants.ACTIVITY_TASK);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                var message = $"Unable to create task '{subject}' for contact '{contact.ContactDescriptiveName}.' Response from the server was: {responseContent}";
                Service.Resolve<IEventLogService>().LogError(nameof(Dynamics365TaskAction), nameof(Execute), message);
            }
        }
    }
}