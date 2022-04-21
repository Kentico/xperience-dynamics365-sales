﻿namespace Kentico.Xperience.Dynamics365.Sales.Constants
{
    /// <summary>
    /// Constants used in the Dynamics 365 integration.
    /// </summary>
    public static class Dynamics365Constants
    {
        /// <summary>
        /// The duration of the cache for Dynamics 365 requests.
        /// </summary>
        public const int CACHE_MINUTES = 60;


        /// <summary>
        /// The code name for the appointment activity type.
        /// </summary>
        public const string ACTIVITY_APPOINTMENT = "appointment";


        /// <summary>
        /// The code name for the task activity type.
        /// </summary>
        public const string ACTIVITY_TASK = "task";


        /// <summary>
        /// The Entity name of Dynamics 365 contacts.
        /// </summary>
        public const string ENTITY_CONTACT = "contact";


        /// <summary>
        /// The Entity name of Dynamics 365 users.
        /// </summary>
        public const string ENTITY_USER = "systemuser";


        /// <summary>
        /// The Entity name of Dynamics 365 teams.
        /// </summary>
        public const string ENTITY_TEAM = "team";


        /// <summary>
        /// The path appended to the Dynamics tenant indicating a Web API request.
        /// </summary>
        public const string ENDPOINT_BASE = "/api/data/v8.2";


        /// <summary>
        /// The Web API endpoint for getting multiple Entities or creating a single Entity.
        /// </summary>
        public const string ENDPOINT_ENTITY_GET_POST = "/{0}s";


        /// <summary>
        /// The Web API endpoint for updating an Entity or retrieving a single Entity.
        /// </summary>
        public const string ENDPOINT_ENTITY_PATCH_GETSINGLE = "/{0}s({1})";


        /// <summary>
        /// The Web API endpoint for retrieving the attributes of an Entity.
        /// </summary>
        public const string ENDPOINT_ENTITY_ATTRIBUTES = "/EntityDefinitions(LogicalName='{0}')/Attributes";


        /// <summary>
        /// The Web API endpoint for retrieving the options for an Entity's status.
        /// </summary>
        public const string ENDPOINT_STATUSCODES = "/EntityDefinitions(LogicalName='{0}')/Attributes(LogicalName='statuscode')/Microsoft.Dynamics.CRM.StatusAttributeMetadata?$select=LogicalName&$expand=OptionSet";


        /// <summary>
        /// The Web API endpoint for retrieving the options for an Entity's state.
        /// </summary>
        public const string ENDPOINT_STATECODES = "/EntityDefinitions(LogicalName='{0}')/Attributes(LogicalName='statecode')/Microsoft.Dynamics.CRM.StateAttributeMetadata?$select=LogicalName&$expand=OptionSet";


        /// <summary>
        /// The Web API endpoint for retrieving the options of an Entity's attribute.
        /// </summary>
        public const string ENDPOINT_OPTIONSET = "/EntityDefinitions(LogicalName='{0}')/Attributes(LogicalName='{1}')/Microsoft.Dynamics.CRM.PicklistAttributeMetadata?$select=LogicalName&$expand=OptionSet";


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