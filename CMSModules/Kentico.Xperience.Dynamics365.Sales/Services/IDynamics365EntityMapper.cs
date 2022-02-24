using CMS.Activities;
using CMS.DataEngine;

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
        /// <param name="activityType"></param>
        /// <returns></returns>
        string MapActivityType(string activityType);


        /// <summary>
        /// Creates an anonymous object from an Xperience activity.
        /// </summary>
        /// <param name="entityName">The name of the Entity that is being created based on the current task/event.</param>
        /// <param name="dynamicsId">The internal Dynamics 365 contact ID associated with the activity.</param>
        /// <param name="activity">The Xperience activity which triggered the Entity creation.</param>
        /// <returns>An object with Dynamics 365 fields and their values.</returns>
        JObject MapEntity(string entityName, string dynamicsId, ActivityInfo activity);


        /// <summary>
        /// Creates an anonymous object from an Xperience object which has a defined field mapping.
        /// </summary>
        /// <param name="entityName">The name of the Entity that is being created based on the current task/event.</param>
        /// <param name="mapping">The mapping definition containing Dynamics 365 fields and their mapped
        /// Xperience fields.</param>
        /// <param name="sourceObject">The Xperience object which triggered the Entity creation.</param>
        /// <returns>An object with Dynamics 365 fields and their values.</returns>
        JObject MapEntity(string entityName, string mapping, BaseInfo sourceObject);
    }
}