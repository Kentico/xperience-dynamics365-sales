using Kentico.Xperience.Dynamics365.Sales.Models;

using System.Net.Http;

namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    /// <summary>
    /// Contains methods for intefacing with the Dynamics 365 Web API.
    /// </summary>
    public interface IDynamics365Client
    {
        /// <summary>
        /// Creates a new "contact" Entity in Dynamics 365 using the field mappings stored in the
        /// settings key.
        /// </summary>
        /// <returns>The response from the Web API.</returns>
        HttpResponseMessage CreateContact();


        /// <summary>
        /// Gets the access token from the Dynamics 365 application which is required for performing
        /// Web API operations.
        /// </summary>
        /// <returns>An access token, or an emtpy string if the required settings are not populated.</returns>
        string GetAccessToken();


        /// <summary>
        /// Gets the structure of the specified Dynamics 365 Entity and its <see cref="DynamicsEntityAttributeModel"/>s.
        /// </summary>
        /// <param name="name">The <b>LogicalName</b> of the Dynamics 365 Entity to retreive.</param>
        /// <returns></returns>
        DynamicsEntityModel GetEntityModel(string name);
    }
}