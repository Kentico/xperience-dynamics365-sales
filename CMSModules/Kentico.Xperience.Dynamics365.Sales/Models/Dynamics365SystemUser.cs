using Kentico.Xperience.Dynamics365.Sales.Constants;

namespace Kentico.Xperience.Dynamics365.Sales.Models
{
    /// <summary>
    /// The model for a Dynamics 365 systemuser object.
    /// </summary>
    /// <remarks>See <see href="https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/systemuser?view=dynamics-ce-odata-9"/>.</remarks>
    public class Dynamics365SystemUser
    {
        /// <summary>
        /// The user's full name.
        /// </summary>
        public string FullName
        {
            get;
            set;
        }


        /// <summary>
        /// The internal Dynamics 365 ID of the user.
        /// </summary>
        public string SystemUserId
        {
            get;
            set;
        }


        /// <summary>
        /// The email address of the user.
        /// </summary>
        public string InternalEmailAddress
        {
            get;
            set;
        }


        /// <summary>
        /// The user type represented by <see cref="AccessModeEnum"/>.
        /// </summary>
        public int AccessMode
        {
            get;
            set;
        }
    }
}