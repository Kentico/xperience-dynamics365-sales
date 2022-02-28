using CMS.Activities;

namespace Kentico.Xperience.Dynamics365.Sales.Models.Activities
{
    /// <summary>
    /// Data pertaining to a "phonecall" activity which is stored in the
    /// <see cref="ActivityInfo.ActivityValue"/> field for use during synchronization.
    /// </summary>
    public class Dynamics365PhoneCallModel
    {
        /// <summary>
        /// The internal Dynamics 365 ID of the contact that received the call.
        /// </summary>
        public string To
        {
            get;
            set;
        }


        /// <summary>
        /// The internal Dynamics 365 ID of the systemuser that made the call.
        /// </summary>
        public string From
        {
            get;
            set;
        }


        /// <summary>
        /// The title of the activity.
        /// </summary>
        public string Subject
        {
            get;
            set;
        }


        /// <summary>
        /// The description of the activity.
        /// </summary>
        public string Description
        {
            get;
            set;
        }

        
        /// <summary>
        /// The phone number that was called.
        /// </summary>
        public string PhoneNumber
        {
            get;
            set;
        }


        /// <summary>
        /// The phone call's duration in minutes.
        /// </summary>
        public int Duration
        {
            get;
            set;
        }
    }
}