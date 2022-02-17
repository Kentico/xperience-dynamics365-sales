namespace Kentico.Xperience.Dynamics365.Sales.Models
{
    /// <summary>
    /// Describes the structure of an Entity attribute.
    /// </summary>
    /// <remarks>See <see href="https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/attributemetadata?view=dynamics-ce-odata-9"/>.</remarks>
    public class DynamicsEntityAttributeModel
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
        public DynamicsEntityAttributeDisplayName DisplayName
        {
            get;
            set;
        }


        /// <summary>
        /// The required level of the attribute when creating/updating the Entity.
        /// </summary>
        /// <remarks>See <see href="https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/attributerequiredlevel?view=dynamics-ce-odata-9"/>.</remarks>
        public DynamicsEntityAttributeRequiredLevel RequiredLevel
        {
            get;
            set;
        }


        /// <summary>
        /// Constant values representing the possible data type for a <see cref="AttributeType"/>.
        /// </summary>
        /// <remarks>See <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributetypecode?view=dynamics-general-ce-9"/>.</remarks>
        public static class AttributeTypes
        {
            public const string BIGINT = "BigInt";
            public const string BOOLEAN = "Boolean";
            public const string DATETIME = "DateTime";
            public const string DECIMAL = "Decimal";
            public const string DOUBLE = "Double";
            public const string INTEGER = "Integer";
            public const string LOOKUP = "Lookup";
            public const string MEMO = "Memo";
            public const string MONEY = "Money";
            public const string PICKLIST = "Picklist";
            public const string STRING = "String";
            public const string UID = "Uniqueidentifier";
            public const string VIRTUAL = "Virtual";
            
        }
    }
}