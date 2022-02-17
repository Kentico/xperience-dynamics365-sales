using CMS;
using CMS.ContactManagement;
using CMS.Core;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Models;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

[assembly: RegisterImplementation(typeof(IDynamics365ContactSync), typeof(DefaultDynamics365ContactSync), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    /// <summary>
    /// The default implementation of <see cref="IDynamics365ContactSync"/>.
    /// </summary>
    public class DefaultDynamics365ContactSync : IDynamics365ContactSync
    {
        private readonly IDynamics365Client dynamics365Client;
        private readonly IEventLogService eventLogService;
        private readonly ISettingsService settingsService;
        private readonly HttpMethod patchMethod = new HttpMethod("PATCH");


        private string DynamicsUrl
        {
            get
            {
                return ValidationHelper.GetString(settingsService[Dynamics365Constants.SETTING_URL], String.Empty);
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDynamics365ContactSync"/> class.
        /// </summary>
        public DefaultDynamics365ContactSync(IDynamics365Client dynamics365Client, ISettingsService settingsService, IEventLogService eventLogService)
        {
            this.dynamics365Client = dynamics365Client;
            this.settingsService = settingsService;
            this.eventLogService = eventLogService;
        }


        public bool SynchronizationEnabled()
        {
            return ValidationHelper.GetBoolean(settingsService[Dynamics365Constants.SETTINGS_CONTACTSENABLED], false)
                && ValidationHelper.GetInteger(settingsService[Dynamics365Constants.SETTINGS_MINSCORE], 0) > 0; 
        }


        public async Task<HttpResponseMessage> CreateContact(ContactInfo contact, JObject data)
        {
            var response = await SendRequest(Dynamics365Constants.ENDPOINT_CONTACTS_GET_POST, HttpMethod.Post, data).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var createdContact = JObject.Parse(responseJson);
                var dynamicsId = createdContact.Value<string>("contactid");
                if (!String.IsNullOrEmpty(dynamicsId))
                {
                    contact.SetValue(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, dynamicsId);
                    contact.Update();
                }
            }

            return response;
        }


        public async Task<SynchronizationResult> SynchronizeContacts(List<ContactInfo> contacts)
        {
            var mappingDefinition = settingsService[Dynamics365Constants.SETTING_FIELDMAPPING];
            if (String.IsNullOrEmpty(mappingDefinition))
            {
                return new SynchronizationResult
                {
                    HasErrors = true,
                    SynchronizationErrors = new List<string>() { "Unable to load contact field mapping. Please check the settings." },
                    UnsynchronizedObjectIdentifiers = contacts.Select(c => $"{c.ContactDescriptiveName} ({c.ContactGUID})")
                };
            }

            var synchronizedContacts = 0;
            var unsyncedContacts = new List<string>();
            var synchronizationErrors = new List<string>();
            foreach (var contact in contacts)
            {
                var entity = GetMappedEntity(contact, mappingDefinition);
                if (entity == null)
                {
                    continue;
                }

                // Send request
                var dynamicsId = contact.GetStringValue(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, String.Empty);
                HttpResponseMessage response;
                if(String.IsNullOrEmpty(dynamicsId))
                {
                    response = await CreateContact(contact, entity).ConfigureAwait(false);
                }
                else
                {
                    response = await UpdateContact(dynamicsId, entity).ConfigureAwait(false);
                }

                // Handle response
                if (response.IsSuccessStatusCode)
                {
                    synchronizedContacts++;
                }
                else
                {
                    unsyncedContacts.Add($"{contact.ContactDescriptiveName} ({contact.ContactGUID})");
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!synchronizationErrors.Contains(responseContent))
                    {
                        synchronizationErrors.Add(responseContent);
                    }
                }
            }

            return new SynchronizationResult
            {
                HasErrors = synchronizationErrors.Count > 0,
                SynchronizationErrors = synchronizationErrors,
                SynchronizedObjectCount = synchronizedContacts,
                UnsynchronizedObjectIdentifiers = unsyncedContacts
            };
        }


        public async Task<HttpResponseMessage> UpdateContact(string dynamicsId, JObject data)
        {
            return await SendRequest(String.Format(Dynamics365Constants.ENDPOINT_CONTACTS_PATCH, dynamicsId), patchMethod, data).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets an object with properties where the property name is the Dynamics field
        /// and the property value is the data mapped from the Xperience contact.
        /// </summary>
        /// <param name="contact">The contact to load the values from.</param>
        /// <param name="mappingDefinition">A JSON string containing the Dynamics fields and
        /// the Xperience field that should be mapped.</param>
        /// <returns></returns>
        private JObject GetMappedEntity(ContactInfo contact, string mappingDefinition)
        {
            var propertiesToRemove = new List<string>();
            var entity = JObject.Parse(mappingDefinition);
            foreach (var entityProperty in entity.Properties())
            {
                var contactPropertyName = entityProperty.Value.Value<string>();
                var contactValue = contact.GetValue(contactPropertyName);
                if (contactValue == null)
                {
                    propertiesToRemove.Add(entityProperty.Name);
                    continue;
                }

                entityProperty.Value = JToken.FromObject(contactValue);
            }

            foreach (var propertyName in propertiesToRemove)
            {
                entity.Remove(propertyName);
            }

            return entity;
        }


        /// <summary>
        /// Sends a request to the Dynamics 365 Web API.
        /// </summary>
        /// <param name="endpoint">The Web API endpoint to send the request to.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="data">The data to send to the endpoint.</param>
        /// <returns>The response from the Web API.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<HttpResponseMessage> SendRequest(string endpoint, HttpMethod method, JObject data)
        {
            if (String.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (String.IsNullOrEmpty(DynamicsUrl))
            {
                throw new InvalidOperationException("The Dynamics 365 URL is not configured.");
            }

            var accessToken = await dynamics365Client.GetAccessToken().ConfigureAwait(false);
            var url = $"{DynamicsUrl}{Dynamics365Constants.ENDPOINT_BASE}{endpoint}";
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = new TimeSpan(0, 2, 0);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
                httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                if (method == HttpMethod.Post)
                {
                    return await httpClient.PostAsJsonAsync(url, data).ConfigureAwait(false);
                }
                else if (method == patchMethod)
                {
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