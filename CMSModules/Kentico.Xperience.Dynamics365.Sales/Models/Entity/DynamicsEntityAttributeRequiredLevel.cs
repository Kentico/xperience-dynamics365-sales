namespace Kentico.Xperience.Dynamics365.Sales.Models
{
    /// <summary>
    /// The required value configuration for a <see cref="DynamicsEntityAttributeModel"/>.
    /// </summary>
    /// <remarks>See <see href="https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/attributerequiredlevel?view=dynamics-ce-odata-9"/>.</remarks>
    public class DynamicsEntityAttributeRequiredLevel
    {
        /// <summary>
        /// The required level for the Entity attribute.
        /// </summary>
        public string Value
        {
            get;
            set;
        }


        /// <summary>
        /// Constant values representing the possible values for <see cref="Value"/>.
        /// </summary>
        /// <remarks>See <see href="https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/attributerequiredlevel?view=dynamics-ce-odata-9"/>.</remarks>
        public static class RequiredTypes
        {
            public const string NONE = "None";
            public const string RECOMMENDED = "Recommended";
            public const string APPLICATION_REQUIRED = "ApplicationRequired";
            public const string SYSTEM_REQUIRED = "SystemRequired";
        }
    }
}