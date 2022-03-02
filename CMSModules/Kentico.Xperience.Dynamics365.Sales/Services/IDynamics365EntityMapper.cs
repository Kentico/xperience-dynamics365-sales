using CMS.Activities;
using CMS.DataEngine;

using Kentico.Xperience.Dynamics365.Sales.Models.Mapping;

using Newtonsoft.Json.Linq;

namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    /// <summary>
    /// Creates anonymous objects from Xperience objects for sending to Dynamics 365.
    /// </summary>
    public interface IDynamics365EntityMapper
    {
        /// <summary>
        /// Gets the Dynamics 365 Entity name to create for the provided Xperience activity type. Allows
        /// you to synchronize Xperience activities to Dynamics 365 if the code names do not match, e.g. if
        /// the Xperience activity is "pageview" and the Dynamics 365 Entity is "pagevisit."
        /// </summary>
        /// <param name="activityType">The Xperience activity type.</param>
        /// <returns>The Dynamics 365 Entity name.</returns>
        string MapActivityType(string activityType);


        /// <summary>
        /// Creates an anonymous object representing a Dynamics 365 activity.
        /// </summary>
        /// <param name="entityName">The name of the Entity that is being created based on the current task/event.</param>
        /// <param name="dynamicsId">The internal Dynamics 365 contact ID associated with the activity.</param>
        /// <param name="relatedData">An object containing the required data for the activity, such as <see cref="ActivityInfo"/>.</param>
        /// <returns>An object with Dynamics 365 fields and their values.</returns>
        JObject MapActivity(string entityName, string dynamicsId, object relatedData);


        /// <summary>
        /// Creates an anonymous object from an Xperience object which has a defined field mapping.
        /// </summary>
        /// <param name="entityName">The name of the Entity that is being created based on the current task/event.</param>
        /// <param name="mapping">The mapping definition containing Dynamics 365 fields and their mapped
        /// Xperience fields.</param>
        /// <param name="sourceObject">The Xperience object which triggered the Entity creation.</param>
        /// <returns>An object with Dynamics 365 fields and their values.</returns>
        JObject MapEntity(string entityName, MappingModel mapping, BaseInfo sourceObject);


        /// <summary>
        /// Creates an anonymous object for updating purposes from an Xperience object which has a defined field mapping.
        /// The returned object should only contain fields that have been updated locally and not updated within Dynamics 365.
        /// </summary>
        /// <param name="entityName">The name of the Entity that is being updated based on the current task/event.</param>
        /// <param name="mapping">The mapping definition containing Dynamics 365 fields and their mapped
        /// Xperience fields.</param>
        /// <param name="dynamicsId">The ID of the existing object within Dynamics 365.</param>
        /// <param name="sourceObject">The Xperience object which triggered the Entity creation.</param>
        /// <returns>An object with the updated Xperience fields mapped to Dynamics 365 fields, or null if there was an error.</returns>
        JObject MapPartialEntity(string entityName, MappingModel mapping, string dynamicsId, BaseInfo sourceObject);
    }
}