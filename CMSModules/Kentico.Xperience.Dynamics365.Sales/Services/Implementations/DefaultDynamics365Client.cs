using CMS;
using CMS.Core;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Models;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Microsoft.IdentityModel.Clients.ActiveDirectory;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        private HttpClient httpClient;
        private readonly ISettingsService settingsService;
        private const string ENDPOINT_BASE = "/api/data/v8.2";
        private const string ENDPOINT_ENTITY = "/EntityDefinitions(LogicalName='{0}')/Attributes?$select=LogicalName,AttributeType,DisplayName,IsPrimaryId,RequiredLevel";


        private string ClientId
        {
            get
            {
                return ValidationHelper.GetString(settingsService["Dynamics365ClientID"], String.Empty);
            }
        }


        private string TenantId
        {
            get
            {
                return ValidationHelper.GetString(settingsService["Dynamics365TenantID"], String.Empty);
            }
        }


        private string ClientSecret
        {
            get
            {
                return ValidationHelper.GetString(settingsService["Dynamics365Secret"], String.Empty);
            }
        }

        
        private string DynamicsUrl
        {
            get
            {
                return ValidationHelper.GetString(settingsService["Dynamics365URL"], String.Empty);
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDynamics365Client"/> class.
        /// </summary>
        public DefaultDynamics365Client(ISettingsService settingsService)
        {
            InitializeHttpClient();
            this.settingsService = settingsService;
        }


        public HttpResponseMessage CreateContact()
        {
            // TODO: Bind ContactInfo properties to Dynamics fields
            throw new NotImplementedException();
        }


        public string GetAccessToken()
        {
            if (String.IsNullOrEmpty(ClientId) || String.IsNullOrEmpty(ClientSecret) || String.IsNullOrEmpty(DynamicsUrl) || String.IsNullOrEmpty(TenantId))
            {
                return String.Empty;
            }

            var authContext = new AuthenticationContext($"https://login.windows.net/{TenantId}", false);
            var auth = authContext.AcquireTokenAsync(DynamicsUrl, new ClientCredential(ClientId, ClientSecret));

            return auth.ConfigureAwait(false).GetAwaiter().GetResult().AccessToken;
        }


        public DynamicsEntityModel GetEntityModel(string name)
        {
            var response = SendRequest(String.Format(ENDPOINT_ENTITY, name), HttpMethod.Get);
            if (!response.IsSuccessStatusCode)
            {
                // TODO: Log error
                return null;
            }

            var sourceJson = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var entity = JsonConvert.DeserializeObject<DynamicsEntityModel>(sourceJson);
            entity.Value = entity.Value.Where(attr =>
                attr.AttributeType != AttributeTypes.PICKLIST
                && attr.AttributeType != AttributeTypes.VIRTUAL
                && attr.AttributeType != AttributeTypes.LOOKUP
                && !attr.IsPrimaryId
            );

            return entity;
        }


        /// <summary>
        /// Sends a request to the Dynamics 365 Web API.
        /// </summary>
        /// <param name="endpoint">The Web API endpoint to send the request to.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="data">Data to be POSTED to the Web API for POST requests.</param>
        /// <returns>The response from the Web API.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private HttpResponseMessage SendRequest(string endpoint, HttpMethod method, JObject data = null)
        {
            if (String.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (String.IsNullOrEmpty(DynamicsUrl))
            {
                throw new InvalidOperationException("The Dynamics 365 URL is not configured.");
            }

            if (method == HttpMethod.Post && data == null)
            {
                throw new InvalidOperationException("Data must be provided when using the POST method.");
            }

            // Refresh access token (recommended by Microsoft before all API calls)
            var accessToken = GetAccessToken();
            if (String.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Unable to obtain the Dynamics 365 access token. Please check the settings.");
            }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var url = $"{DynamicsUrl}{ENDPOINT_BASE}{endpoint}";
            Task<HttpResponseMessage> response = null;
            if (method== HttpMethod.Get)
            {
                response = httpClient.GetAsync(url);
            }
            else if (method == HttpMethod.Post)
            {
                response = httpClient.PostAsJsonAsync(url, data);
            }

            return response.ConfigureAwait(false).GetAwaiter().GetResult();
        }


        private void InitializeHttpClient()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = new TimeSpan(0, 2, 0);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
            httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
        }
    }
}