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
    /// A custom Marketing automation action which creates a new appointment in Dynamics 365.
    /// </summary>
    public class Dynamics365AppointmentAction : AutomationAction
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

            var appointmentModel = new Dynamics365AppointmentModel
            {
                Subject = subject,
                Description = GetResolvedParameter("Description", string.Empty),
                RequiredAttendee = GetResolvedParameter("RequiredAttendee", string.Empty),
                OptionalAttendee = GetResolvedParameter("OptionalAttendee", string.Empty),
                Location = GetResolvedParameter("Location", string.Empty),
                IsAllDay = GetResolvedParameter("IsAllDay", false),
                StartTime = GetResolvedParameter("StartTime", DateTime.MinValue),
                EndTime = GetResolvedParameter("EndTime", DateTime.MinValue)
            };

            var entity = Service.Resolve<IDynamics365EntityMapper>().MapActivity(Dynamics365Constants.ACTIVITY_APPOINTMENT, dynamicsId, appointmentModel);
            var response = Service.Resolve<IDynamics365ActivitySync>().CreateActivity(entity, Dynamics365Constants.ACTIVITY_APPOINTMENT);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                var message = $"Unable to create appointment '{subject}' for contact '{contact.ContactDescriptiveName}.' Response from the server was: {responseContent}";
                Service.Resolve<IEventLogService>().LogError(nameof(Dynamics365AppointmentAction), nameof(Execute), message);
            }
        }
    }
}