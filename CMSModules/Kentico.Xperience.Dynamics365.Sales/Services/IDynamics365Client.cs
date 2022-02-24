using Kentico.Xperience.Dynamics365.Sales.Models;
using Kentico.Xperience.Dynamics365.Sales.Models.Entity;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Net.Http;
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
        /// Gets the specified Entity's <see cref="Dynamics365EntityAttributeModel"/>s.
        /// </summary>
        /// <param name="entityName">The <b>LogicalName</b> of the Dynamics 365 Entity to retreive.</param>
        /// <returns>The Entity's attributes, or an empty list if there was an error retrieving it.</returns>
        Task<IEnumerable<Dynamics365EntityAttributeModel>> GetEntityAttributes(string entityName);


        /// <summary>
        /// Gets the Dynamics 365 systemuser objects.
        /// </summary>
        Task<IEnumerable<Dynamics365SystemUser>> GetSystemUsers();


        /// <summary>
        /// Sends a request to the Dynamics 365 Web API.
        /// </summary>
        /// <param name="endpoint">The Web API endpoint to send the request to.</param>
        /// <param name="method">The method to use in the request.</param>
        /// <param name="data">The data to send in the request.</param>
        /// <returns>The response from the Web API.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        Task<HttpResponseMessage> SendRequest(string endpoint, HttpMethod method, JObject data = null);
    }
}