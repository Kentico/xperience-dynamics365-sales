using CMS.Activities;
using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;
using CMS.EmailEngine;
using CMS.Helpers;
using CMS.Newsletters;
using CMS.SiteProvider;
using CMS.WorkflowEngine;

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
    public class Dynamics365EmailAction : AutomationAction
    {
        public override void Execute()
        {
            var siteId = GetResolvedParameter("Site", 0);
            if (siteId == 0)
            {
                throw new InvalidOperationException("The required properties are not set for the automation step.");
            }

            var contact = InfoObject as ContactInfo;
            var email = GetSentEmail(siteId, contact);
            if (email == null)
            {
                return;
            }

            if (String.IsNullOrEmpty(email.Subject) || String.IsNullOrEmpty(email.To) || String.IsNullOrEmpty(email.From))
            {
                return;
            }

            ConvertEmailAddressesToDynamicsIds(email, contact);

            var activity = new ActivityInfo
            {
                ActivitySiteID = siteId,
                ActivityTitle = email.Subject,
                ActivityType = Dynamics365Constants.ACTIVITY_EMAIL,
                ActivityValue = JsonConvert.SerializeObject(email),
                ActivityContactID = contact.ContactID
            };

            ActivityInfo.Provider.Set(activity);
        }


        private void ConvertEmailAddressesToDynamicsIds(Dynamics365EmailModel email, ContactInfo contact)
        {
            if (email.To.Equals(contact.ContactEmail, StringComparison.OrdinalIgnoreCase))
            {
                var dynamicsId = contact.GetStringValue(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, String.Empty);
                email.To = dynamicsId;
            }
            else
            {
                // Email was sent to someone else, try to get Dynamics user
                var matchingUserId = GetUserWithEmail(email.To);
                email.SentToUser = true;
                email.To = matchingUserId;
            }

            email.From = GetUserWithEmail(email.From);
        }


        private string GetUserWithEmail(string email)
        {
            var systemUsers = Service.Resolve<IDynamics365Client>().GetSystemUsers().ConfigureAwait(false).GetAwaiter().GetResult();
            var matchingUser = systemUsers.FirstOrDefault(user => user.InternalEmailAddress != null && user.InternalEmailAddress.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (matchingUser == null)
            {
                return String.Empty;
            }

            return matchingUser.SystemUserId;
        }


        private Dynamics365EmailModel GetSentEmail(int siteId, ContactInfo contact)
        {
            var emailTo = String.Empty;
            var emailFrom = String.Empty;
            var emailBody = String.Empty;
            var emailSubject = String.Empty;

            var previousStepHistory = AutomationHistoryInfo.Provider.Get()
                .WhereEquals(nameof(AutomationHistoryInfo.HistoryStateID), StateObject.StateID)
                .WhereEquals(nameof(AutomationHistoryInfo.HistoryTargetStepID), ActionStep.StepID)
                .TypedResult
                .FirstOrDefault();

            if (previousStepHistory != null)
            {
                var workflowStep = WorkflowStepInfo.Provider.Get(previousStepHistory.HistoryStepID);
                if (workflowStep.StepName.Equals("SendTransactionalEmail", StringComparison.OrdinalIgnoreCase))
                {
                    emailTo = ValidationHelper.GetString(workflowStep.StepActionParameters["To"], String.Empty);
                    emailFrom = ValidationHelper.GetString(workflowStep.StepActionParameters["From"], String.Empty);

                    var basedOn = ValidationHelper.GetInteger(workflowStep.StepActionParameters["BasedOn"], 0);
                    if (basedOn == 1)
                    {
                        // HTML formatted text
                        emailBody = ValidationHelper.GetString(workflowStep.StepActionParameters["Body"], String.Empty);
                        emailSubject = ValidationHelper.GetString(workflowStep.StepActionParameters["Subject"], String.Empty);
                    }
                    else
                    {
                        // Email template
                        var emailTemplateCodeName = ValidationHelper.GetString(workflowStep.StepActionParameters["EmailTemplate"], String.Empty);
                        var emailTemplate = CMS.EmailEngine.EmailTemplateInfo.Provider.Get(emailTemplateCodeName, siteId);
                        if (emailTemplate != null)
                        {
                            var preferredEmailFormat = EmailHelper.GetEmailFormat(siteId);
                            emailBody = preferredEmailFormat == EmailFormatEnum.PlainText ? emailTemplate.TemplatePlainText : emailTemplate.TemplateText;
                            emailSubject = emailTemplate.TemplateSubject;
                        }
                    }
                }
                else if (workflowStep.StepName.Equals("SendMarketingEmail", StringComparison.OrdinalIgnoreCase))
                {
                    var newsletterIssueGuid = ValidationHelper.GetString(workflowStep.StepActionParameters["NewsletterIssue"], String.Empty);
                    if (!String.IsNullOrEmpty(newsletterIssueGuid))
                    {
                        var newsletterSiteName = ValidationHelper.GetString(workflowStep.StepActionParameters["Site"], String.Empty);
                        var site = SiteInfo.Provider.Get(newsletterSiteName);
                        var issue = IssueInfo.Provider.Get(Guid.Parse(newsletterIssueGuid), site.SiteID);

                        // If contact is unsubscribed, no email was sent, so don't log activity
                        var subscriptionService = Service.Resolve<ISubscriptionService>();
                        if (subscriptionService.IsUnsubscribed(contact.ContactEmail, issue.IssueNewsletterID))
                        {
                            return null;
                        }

                        emailTo = contact.ContactEmail;
                        emailFrom = issue.IssueSenderEmail;
                        emailSubject = issue.IssueSubject;
                    }
                }
            }

            return new Dynamics365EmailModel
            {
                To = this.MacroResolver.ResolveMacros(emailTo),
                From = this.MacroResolver.ResolveMacros(emailFrom),
                Subject = this.MacroResolver.ResolveMacros(emailSubject),
                Body = this.MacroResolver.ResolveMacros(emailBody)
            };
        }
    }
}