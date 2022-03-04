﻿using Kentico.Xperience.Dynamics365.Sales.Models;
using Kentico.Xperience.Dynamics365.Sales.Models.Entity;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Net.Http;

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
        string GetAccessToken();


        /// <summary>
        /// Gets the specified Entity's <see cref="Dynamics365EntityAttributeModel"/>s.
        /// </summary>
        /// <param name="entityName">The <b>LogicalName</b> of the Dynamics 365 Entity to retreive.</param>
        /// <returns>The Entity's attributes, or an empty list if there was an error retrieving it.</returns>
        IEnumerable<Dynamics365EntityAttributeModel> GetEntityAttributes(string entityName);


        /// <summary>
        /// Gets an Entity definition from Dynamics 365. The available data in the returned model is
        /// dependent on the <paramref name="endpoint"/> used.
        /// </summary>
        /// <param name="entityName">The fully-resolved Dynamics 365 Web API endpoint.</param>
        /// <returns>The Entity, or null if there were errors.</returns>
        Dynamics365EntityModel GetEntity(string endpoint);


        /// <summary>
        /// Gets the Dynamics 365 systemuser objects, or an empty enumerable.
        /// </summary>
        IEnumerable<Dynamics365SystemUser> GetSystemUsers();


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
        /// <exception cref="HttpRequestException"></exception>
        HttpResponseMessage SendRequest(string endpoint, HttpMethod method, JObject data = null);
    }
}