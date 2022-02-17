using CMS;
using CMS.Core;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Models;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Microsoft.IdentityModel.Clients.ActiveDirectory;

using Newtonsoft.Json;

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using static Kentico.Xperience.Dynamics365.Sales.Models.DynamicsEntityAttributeModel;

[assembly: RegisterImplementation(typeof(IDynamics365Client), typeof(DefaultDynamics365Client), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    /// <summary>
    /// The default implementation of <see cref="IDynamics365Client"/>.
    /// </summary>
    public class DefaultDynamics365Client : IDynamics365Client
    {
        private readonly ISettingsService settingsService;
        private readonly IEventLogService eventLogService;
        

        private string DynamicsUrl
        {
            get
            {
                return ValidationHelper.GetString(settingsService[Dynamics365Constants.SETTING_URL], String.Empty);
            }
        }


        private string ClientId
        {
            get
            {
                return ValidationHelper.GetString(settingsService[Dynamics365Constants.SETTING_CLIENTID], String.Empty);
            }
        }


        private string TenantId
        {
            get
            {
                return ValidationHelper.GetString(settingsService[Dynamics365Constants.SETTING_TENANTID], String.Empty);
            }
        }


        private string ClientSecret
        {
            get
            {
                return ValidationHelper.GetString(settingsService[Dynamics365Constants.SETTING_SECRET], String.Empty);
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDynamics365Client"/> class.
        /// </summary>
        public DefaultDynamics365Client(ISettingsService settingsService, IEventLogService eventLogService)
        {
            this.settingsService = settingsService;
            this.eventLogService = eventLogService;
        }


        public async Task<string> GetAccessToken()
        {
            if (String.IsNullOrEmpty(ClientId) || String.IsNullOrEmpty(ClientSecret) || String.IsNullOrEmpty(DynamicsUrl) || String.IsNullOrEmpty(TenantId))
            {
                return String.Empty;
            }

            var authContext = new AuthenticationContext($"https://login.windows.net/{TenantId}", false);
            var auth = await authContext.AcquireTokenAsync(DynamicsUrl, new ClientCredential(ClientId, ClientSecret)).ConfigureAwait(false);

            return auth.AccessToken;
        }


        public async Task<DynamicsEntityModel> GetEntityModel(string name)
        {
            var response = await SendGetRequest(String.Format(Dynamics365Constants.ENDPOINT_ENTITY, name)).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                eventLogService.LogError(nameof(DefaultDynamics365Client), nameof(GetEntityModel), responseContent);

                return null;
            }

            var sourceJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var entity = JsonConvert.DeserializeObject<DynamicsEntityModel>(sourceJson);
            entity.Value = entity.Value.Where(attr =>
                attr.AttributeType != AttributeTypes.PICKLIST
                && attr.AttributeType != AttributeTypes.VIRTUAL
                && attr.AttributeType != AttributeTypes.LOOKUP
                && !attr.IsPrimaryId
            ).OrderBy(attr => attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName);

            return entity;
        }


        /// <summary>
        /// Sends a GET request to the Dynamics 365 Web API.
        /// </summary>
        /// <param name="endpoint">The Web API endpoint to send the request to.</param>
        /// <returns>The response from the Web API.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<HttpResponseMessage> SendGetRequest(string endpoint)
        {
            if (String.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (String.IsNullOrEmpty(DynamicsUrl))
            {
                throw new InvalidOperationException("The Dynamics 365 URL is not configured.");
            }

            var accessToken = await GetAccessToken().ConfigureAwait(false);
            var url = $"{DynamicsUrl}{Dynamics365Constants.ENDPOINT_BASE}{endpoint}";
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = new TimeSpan(0, 2, 0);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
                httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                return await httpClient.GetAsync(url).ConfigureAwait(false);
            }
        }
    }
}