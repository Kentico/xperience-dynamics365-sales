namespace Kentico.Xperience.Dynamics365.Sales.Models.Entity
{
    /// <summary>
    /// Describes the structure of an Entity attribute.
    /// </summary>
    /// <remarks>See <see href="https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/attributemetadata?view=dynamics-ce-odata-9"/>.</remarks>
    public class Dynamics365EntityAttributeModel
    {
        /// <summary>
        /// The internal Dynamics attribute name.
        /// </summary>
        public string LogicalName
        {
            get;
            set;
        }


        /// <summary>
        /// The data type of the attribute, which is one of <see cref="AttributeTypes"/>.
        /// </summary>
        public string AttributeType
        {
            get;
            set;
        }


        /// <summary>
        /// True if the attribute is a primary key.
        /// </summary>
        public bool IsPrimaryId
        {
            get;
            set;
        }


        /// <summary>
        /// The human-friendly label for the attribute.
        /// </summary>
        /// <remarks>See <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.entitymetadata.displayname?view=dynamics-general-ce-9"/>.</remarks>
        public Dynamics365Label DisplayName
        {
            get;
            set;
        }


        /// <summary>
        /// The required level of the attribute when creating/updating the Entity.
        /// </summary>
        /// <remarks>See <see href="https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/attributerequiredlevel?view=dynamics-ce-odata-9"/>.</remarks>
        public Dynamics365EntityAttributeRequiredLevel RequiredLevel
        {
            get;
            set;
        }
    }
}