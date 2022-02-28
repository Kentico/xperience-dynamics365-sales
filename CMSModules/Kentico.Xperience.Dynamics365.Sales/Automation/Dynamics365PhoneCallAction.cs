using CMS.Activities;
using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;
using CMS.Membership;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models.Activities;
using Kentico.Xperience.Dynamics365.Sales.Models.Entity;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json;

using System;
using System.Linq;

namespace Kentico.Xperience.Dynamics365.Sales.Automation
{
    /// <summary>
    /// A custom Marketing automation action which logs a "phonecall" activity. 
    /// </summary>
    public class Dynamics365PhoneCallAction : AutomationAction
    {
        private IDynamics365Client dynamics365Client;


        public override void Execute()
        {
            var subject = GetResolvedParameter("Subject", String.Empty);
            var siteId = GetResolvedParameter("Site", 0);

            if (String.IsNullOrEmpty(subject) || siteId == 0)
            {
                throw new InvalidOperationException("The required properties are not set for the automation step.");
            }

            dynamics365Client = Service.Resolve<IDynamics365Client>();
            var contact = InfoObject as ContactInfo;
            var callTo = contact.GetStringValue(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, String.Empty);
            var phoneNumber = String.IsNullOrEmpty(contact.ContactBusinessPhone) ? contact.ContactMobilePhone : contact.ContactBusinessPhone;
            var defaultDuration = GetResolvedParameter("DefaultDuration", 10);

            var previousStepHistory = AutomationHistoryInfo.Provider.Get()
                .WhereEquals(nameof(AutomationHistoryInfo.HistoryStateID), StateObject.StateID)
                .WhereEquals(nameof(AutomationHistoryInfo.HistoryTargetStepID), ActionStep.StepID)
                .TypedResult
                .FirstOrDefault();

            var activityData = new Dynamics365PhoneCallModel
            {
                To = callTo,
                From = GetApprover(previousStepHistory),
                Subject = subject,
                PhoneNumber = phoneNumber,
                Description = GetComment(previousStepHistory),
                Duration = defaultDuration
            };

            var activity = new ActivityInfo
            {
                ActivitySiteID = siteId,
                ActivityTitle = subject,
                ActivityType = Dynamics365Constants.ACTIVITY_PHONECALL,
                ActivityValue = JsonConvert.SerializeObject(activityData),
                ActivityContactID = contact.ContactID
            };

            ActivityInfo.Provider.Set(activity);
        }


        private string GetComment(AutomationHistoryInfo previousStep)
        {
            string comment = String.Empty;
            if (previousStep != null)
            {
                if (!String.IsNullOrEmpty(previousStep.HistoryComment))
                {
                    comment = previousStep.HistoryComment;
                }
            }

            return this.MacroResolver.ResolveMacros(comment);
        }


        private string GetApprover(AutomationHistoryInfo previousStep)
        {
            // Try to get phone call 'From' user from previous step
            string userEmail = String.Empty;
            if (previousStep != null)
            {
                var user = UserInfo.Provider.Get(previousStep.HistoryApprovedByUserID);
                if (user != null)
                {
                    userEmail = user.Email;
                }
            }

            if (String.IsNullOrEmpty(userEmail))
            {
                return String.Empty;
            }

            // Find Dynamics user with same email address
            var systemUsers = dynamics365Client.GetSystemUsers();
            var matchingUser = systemUsers.FirstOrDefault(user => user.InternalEmailAddress.Equals(userEmail, StringComparison.OrdinalIgnoreCase));
            if (matchingUser == null)
            {
                return String.Empty;
            }

            return matchingUser.SystemUserId;
        }
    }
}