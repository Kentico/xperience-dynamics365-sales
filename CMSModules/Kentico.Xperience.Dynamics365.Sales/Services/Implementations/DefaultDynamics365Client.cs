using CMS;
using CMS.Core;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models;
using Kentico.Xperience.Dynamics365.Sales.Models.Entity;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Microsoft.IdentityModel.Clients.ActiveDirectory;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

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
        private readonly IProgressiveCache progressiveCache;
        private readonly HttpMethod patchMethod = new HttpMethod("PATCH");


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
        public DefaultDynamics365Client(
            ISettingsService settingsService,
            IEventLogService eventLogService,
            IProgressiveCache progressiveCache)
        {
            this.settingsService = settingsService;
            this.eventLogService = eventLogService;
            this.progressiveCache = progressiveCache;
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


        public async Task<IEnumerable<Dynamics365EntityAttributeModel>> GetEntityAttributes(string entityName)
        {
            var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_ATTRIBUTES, entityName);
            var response = await SendRequest(endpoint, HttpMethod.Get).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                eventLogService.LogError(nameof(DefaultDynamics365Client), nameof(GetEntityAttributes), responseContent);

                return null;
            }

            var sourceJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var jObject = JObject.Parse(sourceJson);
            var attributes = JsonConvert.DeserializeObject<IEnumerable<Dynamics365EntityAttributeModel>>(jObject.Value<JArray>("value").ToString());
            return attributes.Where(attr =>
                attr.AttributeType != EntityAttributeType.PICKLIST
                && attr.AttributeType != EntityAttributeType.VIRTUAL
                && attr.AttributeType != EntityAttributeType.LOOKUP
                && !attr.IsPrimaryId
            ).OrderBy(attr => attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName);
        }


        public async Task<Dynamics365EntityModel> GetEntity(string endpoint)
        {
            var response = await SendRequest(endpoint, HttpMethod.Get).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<Dynamics365EntityModel>(responseContent);
            }
            else
            {
                eventLogService.LogError(nameof(DefaultDynamics365Client), nameof(GetEntity), responseContent);
                return null;
            }
        }


        public async Task<IEnumerable<Dynamics365SystemUser>> GetSystemUsers()
        {
            var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_GET_POST, "systemuser") + "?$select=systemuserid,internalemailaddress,fullname,accessmode";
            var response = await SendRequest(endpoint, HttpMethod.Get).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var jObject = JObject.Parse(responseContent);
                var userArray = jObject.Value<JArray>("value");

                return JsonConvert.DeserializeObject<IEnumerable<Dynamics365SystemUser>>(userArray.ToString());
            }
            else
            {
                eventLogService.LogError(nameof(DefaultDynamics365Client), nameof(GetSystemUsers), responseContent);
                return Enumerable.Empty<Dynamics365SystemUser>();
            }
        }

        
        public async Task<HttpResponseMessage> SendRequest(string endpoint, HttpMethod method, JObject data = null)
        {
            if (String.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (String.IsNullOrEmpty(DynamicsUrl))
            {
                throw new InvalidOperationException("The Dynamics 365 URL is not configured.");
            }

            if ((method == HttpMethod.Post || method == patchMethod) && data == null)
            {
                throw new InvalidOperationException("Data must be provided for POST and PATCH methods.");
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

                if (method == HttpMethod.Get)
                {
                    return await httpClient.GetAsync(url).ConfigureAwait(false);
                }
                else if (method == HttpMethod.Post)
                {
                    httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

                    return await httpClient.PostAsJsonAsync(url, data).ConfigureAwait(false);
                }
                else if (method == patchMethod)
                {
                    httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
                    var request = new HttpRequestMessage(patchMethod, url);
                    request.Content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");

                    return await httpClient.SendAsync(request).ConfigureAwait(false);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}