using Kentico.Xperience.Dynamics365.Sales.Models;

using System.Threading.Tasks;

namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    /// <summary>
    /// Contains methods for authenticating with Dynamics 365 and obtaining Entity information.
    /// </summary>
    public interface IDynamics365Client
    {
        /// <summary>
        /// Gets the access token from the Dynamics 365 application which is required for performing
        /// Web API operations.
        /// </summary>
        /// <returns>An access token, or an emtpy string if the required settings are not populated.</returns>
        Task<string> GetAccessToken();


        /// <summary>
        /// Gets the structure of the specified Dynamics 365 Entity and its <see cref="DynamicsEntityAttributeModel"/>s.
        /// </summary>
        /// <param name="name">The <b>LogicalName</b> of the Dynamics 365 Entity to retreive.</param>
        /// <returns>The Entity definition, or null if there was an error retrieving it.</returns>
        Task<DynamicsEntityModel> GetEntityModel(string name);
    }
}