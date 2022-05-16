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
        private readonly IAppSettingsService appSettingsService;
        private readonly HttpMethod patchMethod = new HttpMethod("PATCH");


        private string DynamicsUrl
        {
            get
            {
                return ValidationHelper.GetString(appSettingsService[Dynamics365Constants.APPSETTING_URL], String.Empty);
            }
        }


        private string ClientId
        {
            get
            {
                return ValidationHelper.GetString(appSettingsService[Dynamics365Constants.APPSETTING_CLIENTID], String.Empty);
            }
        }


        private string TenantId
        {
            get
            {
                return ValidationHelper.GetString(appSettingsService[Dynamics365Constants.APPSETTING_TENANTID], String.Empty);
            }
        }


        private string ClientSecret
        {
            get
            {
                return ValidationHelper.GetString(appSettingsService[Dynamics365Constants.APPSETTING_SECRET], String.Empty);
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDynamics365Client"/> class.
        /// </summary>
        public DefaultDynamics365Client(
            ISettingsService settingsService,
            IEventLogService eventLogService,
            IProgressiveCache progressiveCache,
            IAppSettingsService appSettingsService)
        {
            this.settingsService = settingsService;
            this.eventLogService = eventLogService;
            this.progressiveCache = progressiveCache;
            this.appSettingsService = appSettingsService;
        }


        public string GetAccessToken()
        {
            if (String.IsNullOrEmpty(ClientId) || String.IsNullOrEmpty(ClientSecret) || String.IsNullOrEmpty(DynamicsUrl) || String.IsNullOrEmpty(TenantId))
            {
                return String.Empty;
            }

            var authContext = new AuthenticationContext($"https://login.windows.net/{TenantId}", false);
            var auth = authContext.AcquireTokenAsync(DynamicsUrl, new ClientCredential(ClientId, ClientSecret)).ConfigureAwait(false).GetAwaiter().GetResult();

            return auth.AccessToken;
        }


        public IEnumerable<Dynamics365EntityAttributeModel> GetEntityAttributes(string entityName)
        {
            if (String.IsNullOrEmpty(entityName))
            {
                throw new ArgumentNullException(nameof(entityName));
            }

            return progressiveCache.Load(cacheSettings => {
                try
                {
                    var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_ATTRIBUTES, entityName);
                    var response = SendRequest(endpoint, HttpMethod.Get);
                    if (response.IsSuccessStatusCode)
                    {
                        var sourceJson = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        var jObject = JObject.Parse(sourceJson);
                        var attributes = JsonConvert.DeserializeObject<IEnumerable<Dynamics365EntityAttributeModel>>(jObject.Value<JArray>("value").ToString());
                        return attributes.Where(attr =>
                            attr.AttributeType != EntityAttributeType.PICKLIST
                            && attr.AttributeType != EntityAttributeType.VIRTUAL
                            && attr.AttributeType != EntityAttributeType.LOOKUP
                            && !attr.IsPrimaryId
                        ).OrderBy(attr => attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName);
                    }
                    else
                    {
                        var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        eventLogService.LogError(nameof(DefaultDynamics365Client), nameof(GetEntityAttributes), responseContent);
                        cacheSettings.Cached = false;

                        return Enumerable.Empty<Dynamics365EntityAttributeModel>();
                    }
                }
                catch (Exception ex)
                {
                    eventLogService.LogError(nameof(DefaultDynamics365Client), nameof(GetEntityAttributes), ex.Message);
                    cacheSettings.Cached = false;

                    return Enumerable.Empty<Dynamics365EntityAttributeModel>();
                }
                
            }, new CacheSettings(TimeSpan.FromMinutes(Dynamics365Constants.CACHE_MINUTES).TotalMinutes, $"{nameof(DefaultDynamics365Client)}|{nameof(GetEntityAttributes)}|{entityName}"));
        }


        public Dynamics365EntityModel GetEntity(string endpoint)
        {
            if (String.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            return progressiveCache.Load(cacheSettings => {
                try
                {
                    var response = SendRequest(endpoint, HttpMethod.Get);
                    var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<Dynamics365EntityModel>(responseContent);
                    }
                    else
                    {
                        cacheSettings.Cached = false;
                        eventLogService.LogError(nameof(DefaultDynamics365Client), nameof(GetEntity), responseContent);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    cacheSettings.Cached = false;
                    eventLogService.LogError(nameof(DefaultDynamics365Client), nameof(GetEntity), ex.Message);
                    return null;
                }
            }, new CacheSettings(TimeSpan.FromMinutes(Dynamics365Constants.CACHE_MINUTES).TotalMinutes, $"{nameof(DefaultDynamics365Client)}|{nameof(GetEntity)}|{endpoint}"));
        }


        public IEnumerable<Dynamics365SystemUser> GetSystemUsers()
        {
            return progressiveCache.Load(cacheSettings => {
                try
                {
                    var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_GET_POST, Dynamics365Constants.ENTITY_USER) + "?$select=systemuserid,internalemailaddress,fullname,accessmode";
                    var response = SendRequest(endpoint, HttpMethod.Get);
                    var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    if (response.IsSuccessStatusCode)
                    {
                        var jObject = JObject.Parse(responseContent);
                        var userArray = jObject.Value<JArray>("value");
                        return JsonConvert.DeserializeObject<IEnumerable<Dynamics365SystemUser>>(userArray.ToString());
                    }
                    else
                    {
                        cacheSettings.Cached = false;
                        eventLogService.LogError(nameof(DefaultDynamics365Client), nameof(GetSystemUsers), responseContent);
                        return Enumerable.Empty<Dynamics365SystemUser>();
                    }
                }
                catch (Exception ex)
                {
                    cacheSettings.Cached = false;
                    eventLogService.LogError(nameof(DefaultDynamics365Client), nameof(GetSystemUsers), ex.Message);
                    return Enumerable.Empty<Dynamics365SystemUser>();
                }

            }, new CacheSettings(TimeSpan.FromMinutes(Dynamics365Constants.CACHE_MINUTES).TotalMinutes, $"{nameof(DefaultDynamics365Client)}|{nameof(GetSystemUsers)}"));
        }

        
        public HttpResponseMessage SendRequest(string endpoint, HttpMethod method, JObject data = null)
        {
            if (String.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (String.IsNullOrEmpty(DynamicsUrl) || String.IsNullOrEmpty(ClientId) || String.IsNullOrEmpty(TenantId) || String.IsNullOrEmpty(ClientSecret))
            {
                throw new InvalidOperationException("The web.config application settings are not properly configured.");

            }

            if ((method == HttpMethod.Post || method == patchMethod) && data == null)
            {
                throw new InvalidOperationException("Data must be provided for POST and PATCH methods.");
            }

            var accessToken = GetAccessToken();
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
                    return httpClient.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                else if (method == HttpMethod.Post)
                {
                    httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

                    return httpClient.PostAsJsonAsync(url, data).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                else if (method == patchMethod)
                {
                    httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
                    var request = new HttpRequestMessage(patchMethod, url);
                    request.Content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");

                    return httpClient.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}