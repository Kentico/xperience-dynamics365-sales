using CMS.Activities;

namespace Kentico.Xperience.Dynamics365.Sales.Models.Activities
{
    /// <summary>
    /// Data pertaining to an "email" activity which is stored in the
    /// <see cref="ActivityInfo.ActivityValue"/> field for use during synchronization.
    /// </summary>
    public class Dynamics365EmailModel
    {
        /// <summary>
        /// True if the email was sent to a Dynamics systemuser rather than a contact.
        /// </summary>
        public bool SentToUser
        {
            get;
            set;
        }


        /// <summary>
        /// The internal Dynamics 365 ID of the contact/user that received the email. If empty,
        /// it will be set to the linked Dynamics 365 contact when the activity is synchronized.
        /// </summary>
        public string To
        {
            get;
            set;
        }


        /// <summary>
        /// The internal Dynamics 365 ID of the systemuser that sent the email.
        /// </summary>
        public string From
        {
            get;
            set;
        }


        /// <summary>
        /// The subject of the email.
        /// </summary>
        public string Subject
        {
            get;
            set;
        }


        /// <summary>
        /// The body of the email.
        /// </summary>
        public string Body
        {
            get;
            set;
        }
    }
}