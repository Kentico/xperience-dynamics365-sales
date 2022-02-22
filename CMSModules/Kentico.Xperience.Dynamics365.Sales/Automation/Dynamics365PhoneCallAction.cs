using CMS.Activities;
using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;
using CMS.Membership;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models.Activities;
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
        public override void Execute()
        {
            var subject = GetResolvedParameter("Subject", String.Empty);
            var siteId = GetResolvedParameter("Site", 0);

            if (String.IsNullOrEmpty(subject) || siteId == 0)
            {
                throw new InvalidOperationException("The required properties are not set for the automation step.");
            }

            
            var contact = ContactInfo.Provider.Get(StateObject.StateObjectID);
            var callTo = contact.GetStringValue(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, String.Empty);
            var phoneNumber = String.IsNullOrEmpty(contact.ContactBusinessPhone) ? contact.ContactMobilePhone : contact.ContactBusinessPhone;
            var activityData = new Dynamics365PhoneCallModel
            {
                To = callTo,
                From = GetApprover(),
                Subject = subject,
                PhoneNumber = phoneNumber,
                Description = GetComment()
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


        private string GetComment()
        {
            string comment = String.Empty;
            var previousStepHistoryId = AutomationManager.GetPreviousStepInfo(InfoObject, StateObject).RelatedHistoryID;
            var history = AutomationHistoryInfo.Provider.Get(previousStepHistoryId);
            if (history != null)
            {
                if (!String.IsNullOrEmpty(history.HistoryComment))
                {
                    comment = history.HistoryComment;
                }
            }

            return this.MacroResolver.ResolveMacros(comment);
        }


        private string GetApprover()
        {
            // Try to get phone call 'From' user from previous step
            string userEmail = String.Empty;
            var previousStepHistoryId = AutomationManager.GetPreviousStepInfo(InfoObject, StateObject).RelatedHistoryID;
            var history = AutomationHistoryInfo.Provider.Get(previousStepHistoryId);
            if (history != null)
            {
                var user = UserInfo.Provider.Get(history.HistoryApprovedByUserID);
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
            var systemUsers = Service.Resolve<IDynamics365Client>().GetSystemUsers().ConfigureAwait(false).GetAwaiter().GetResult();
            var matchingUser = systemUsers.FirstOrDefault(user => user.InternalEmailAddress.Equals(userEmail, StringComparison.OrdinalIgnoreCase));
            if (matchingUser == null)
            {
                return String.Empty;
            }

            return matchingUser.SystemUserId;
        }
    }
}