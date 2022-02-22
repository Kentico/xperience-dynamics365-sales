namespace Kentico.Xperience.Dynamics365.Sales.Models.Entity
{
    /// <summary>
    /// The required value configuration for a <see cref="Dynamics365EntityAttributeModel"/>.
    /// </summary>
    /// <remarks>See <see href="https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/attributerequiredlevel?view=dynamics-ce-odata-9"/>.</remarks>
    public class Dynamics365EntityAttributeRequiredLevel
    {
        /// <summary>
        /// The required level for the Entity attribute.
        /// </summary>
        public string Value
        {
            get;
            set;
        }
    }
}