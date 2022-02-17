namespace Kentico.Xperience.Dynamics365.Sales
{
    /// <summary>
    /// Constants used in the Dynamics 365 integration.
    /// </summary>
    public static class Dynamics365Constants
    {
        /// <summary>
        /// The path appended to the Dynamics tenant indicating a Web API request.
        /// </summary>
        public const string ENDPOINT_BASE = "/api/data/v8.2";


        /// <summary>
        /// The Web API endpoint for getting or creating contacts.
        /// </summary>
        public const string ENDPOINT_CONTACTS_GET_POST = "/contacts";


        /// <summary>
        /// The Web API endpoint for updating contacts.
        /// </summary>
        public const string ENDPOINT_CONTACTS_PATCH = "/contacts({0})";


        /// <summary>
        /// The Web API endpoint for retrieving the OData of an Entity.
        /// </summary>
        public const string ENDPOINT_ENTITY = "/EntityDefinitions(LogicalName='{0}')/Attributes?$select=LogicalName,AttributeType,DisplayName,IsPrimaryId,RequiredLevel";


        /// <summary>
        /// The key of the setting containing the Dynamics tenant URL.
        /// </summary> 
        public const string SETTING_URL = "Dynamics365URL";


        /// <summary>
        /// The key of the setting containing the Azure application client ID.
        /// </summary>
        public const string SETTING_CLIENTID = "Dynamics365ClientID";


        /// <summary>
        /// The key of the setting containing the Azure application tenant ID.
        /// </summary>
        public const string SETTING_TENANTID = "Dynamics365TenantID";


        /// <summary>
        /// The key of the setting containing Azure application secret.
        /// </summary>
        public const string SETTING_SECRET = "Dynamics365Secret";


        /// <summary>
        /// The key of the setting containing the Xperience and Dynamics 365 field mappings.
        /// </summary>
        public const string SETTING_FIELDMAPPING = "Dynamics365ContactMapping";


        /// <summary>
        /// The key of the setting containing the minimum score for contact synchronization.
        /// </summary>
        public const string SETTINGS_MINSCORE = "Dynamics365MinScore";


        /// <summary>
        /// The key of the setting indicating whether contact synchronization is enabled.
        /// </summary>
        public const string SETTINGS_CONTACTSENABLED = "Dynamics365ContactSyncEnabled";


        /// <summary>
        /// The name of the contact custom field which contains the identifier of the
        /// Dynamics 365 contact that is related to the Xperience contact.
        /// </summary>
        public const string CUSTOMFIELDS_LINKEDID = "ContactDynamics365RelatedID";
    }
}