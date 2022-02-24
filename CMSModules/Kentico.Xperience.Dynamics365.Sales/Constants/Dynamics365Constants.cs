namespace Kentico.Xperience.Dynamics365.Sales.Constants
{
    /// <summary>
    /// Constants used in the Dynamics 365 integration.
    /// </summary>
    public static class Dynamics365Constants
    {
        /// <summary>
        /// The number of minutes data retrieved from Dynamics 365 is cached.
        /// </summary>
        public const int CACHE_DURATION = 30;


        /// <summary>
        /// The code name for the phone call activity type.
        /// </summary>
        public const string ACTIVITY_PHONECALL = "phonecall";


        /// <summary>
        /// The code name for the email activity type.
        /// </summary>
        public const string ACTIVITY_EMAIL = "email";


        /// <summary>
        /// The path appended to the Dynamics tenant indicating a Web API request.
        /// </summary>
        public const string ENDPOINT_BASE = "/api/data/v8.2";


        /// <summary>
        /// The Web API endpoint for getting or creating Entities.
        /// </summary>
        public const string ENDPOINT_ENTITY_GET_POST = "/{0}s";


        /// <summary>
        /// The Web API endpoint for updating Entities.
        /// </summary>
        public const string ENDPOINT_ENTITY_PATCH = "/{0}s({1})";


        /// <summary>
        /// The Web API endpoint for retrieving the attributes of an Entity.
        /// </summary>
        public const string ENDPOINT_ENTITY_ATTRIBUTES = "/EntityDefinitions(LogicalName='{0}')/Attributes?$select=LogicalName,AttributeType,DisplayName,IsPrimaryId,RequiredLevel";


        /// <summary>
        /// The Web API endpoint which finds all Entities that are activities
        /// </summary>
        public const string ENDPOINT_GET_ACTIVITIES = "/EntityDefinitions?$filter=IsActivity eq true&$select=LogicalName";


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
        /// The internal ID of the default owner for activities.
        /// </summary>
        public const string SETTING_DEFAULTOWNER = "Dynamics365DefaultOwner";


        /// <summary>
        /// The key of the setting indicating whether activity synchronization is enabled.
        /// </summary>
        public const string SETTING_ACTIVITIESENABLED = "Dynamics365ActivitySyncEnabled";


        /// <summary>
        /// The name of the contact custom field which contains the identifier of the
        /// Dynamics 365 contact that is related to the Xperience contact.
        /// </summary>
        public const string CUSTOMFIELDS_LINKEDID = "ContactDynamics365RelatedID";


        /// <summary>
        /// The name of the contact custom field which contains the time that the contact was
        /// first synchronized to Dynamics 365.
        /// </summary>
        public const string CUSTOMFIELDS_SYNCEDON = "ContactDynamics365SynchronizedOn";
    }
}