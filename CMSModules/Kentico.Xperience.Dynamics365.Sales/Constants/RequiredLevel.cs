using Kentico.Xperience.Dynamics365.Sales.Models.Entity;

namespace Kentico.Xperience.Dynamics365.Sales.Constants
{
    /// <summary>
    /// Constant values representing the possible values for <see cref="Dynamics365EntityAttributeRequiredLevel.Value"/>.
    /// </summary>
    /// <remarks>See <see href="https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/attributerequiredlevel?view=dynamics-ce-odata-9"/>.</remarks>
    public static class RequiredLevel
    {
        public const string NONE = "None";
        public const string RECOMMENDED = "Recommended";
        public const string APPLICATION_REQUIRED = "ApplicationRequired";
        public const string SYSTEM_REQUIRED = "SystemRequired";
    }
}