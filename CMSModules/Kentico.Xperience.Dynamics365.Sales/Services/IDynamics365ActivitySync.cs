using CMS.Activities;

using Kentico.Xperience.Dynamics365.Sales.Models;
using Kentico.Xperience.Dynamics365.Sales.Models.Entity;

using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    /// <summary>
    /// Contains methods for synchronizing Xperience activities with Dynamics 365.
    /// </summary>
    public interface IDynamics365ActivitySync
    {
        /// <summary>
        /// Creates a new activity in Dynamics 365.
        /// </summary>
        /// <param name="activityType">The Dynamics Entity name to create.</param>
        /// <param name="data">An object containing the Dynamics 365 fields and the values
        /// from the Xperience activity.</param>
        /// <returns>The response from the Web API.</returns>
        Task<HttpResponseMessage> CreateActivity(JObject data, string activityType);


        /// <summary>
        /// Gets all Entities in Dynamics which are marked as activities.
        /// </summary>
        Task<IEnumerable<Dynamics365EntityModel>> GetAllActivities();


        /// <summary>
        /// Creates records in Dynamics that are linked to the specified contact.
        /// </summary>
        /// <param name="dynamicsId">The internal Dynamics id that the activities will be linked to.</param>
        /// <param name="activities">The activities to create.</param>
        /// <returns></returns>
        Task<SynchronizationResult> SynchronizeActivities(string dynamicsId, IEnumerable<ActivityInfo> activities);


        /// <summary>
        /// Returns true if activities should be synchronized according to the settings.
        /// </summary>
        bool SynchronizationEnabled();
    }
}