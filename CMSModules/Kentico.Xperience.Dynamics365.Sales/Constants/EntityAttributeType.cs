using Kentico.Xperience.Dynamics365.Sales.Models.Entity;

namespace Kentico.Xperience.Dynamics365.Sales.Constants
{
    /// <summary>
    /// Constant values representing the possible data type for a <see cref="Dynamics365EntityAttributeModel.AttributeType"/>.
    /// </summary>
    /// <remarks>See <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributetypecode?view=dynamics-general-ce-9"/>.</remarks>
    public static class EntityAttributeType
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