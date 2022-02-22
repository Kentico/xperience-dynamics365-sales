using CMS;
using CMS.ContactManagement;
using CMS.Core;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        private readonly IDynamics365EntityMapper dynamics365EntityMapper;
        private readonly ISettingsService settingsService;


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDynamics365ContactSync"/> class.
        /// </summary>
        public DefaultDynamics365ContactSync(IDynamics365Client dynamics365Client, IDynamics365EntityMapper dynamics365EntityMapper, ISettingsService settingsService)
        {
            this.dynamics365Client = dynamics365Client;
            this.dynamics365EntityMapper = dynamics365EntityMapper;
            this.settingsService = settingsService;
        }


        public bool SynchronizationEnabled()
        {
            return ValidationHelper.GetBoolean(settingsService[Dynamics365Constants.SETTINGS_CONTACTSENABLED], false)
                && ValidationHelper.GetInteger(settingsService[Dynamics365Constants.SETTINGS_MINSCORE], 0) > 0; 
        }


        public async Task<HttpResponseMessage> CreateContact(ContactInfo contact, JObject data)
        {
            var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_GET_POST, "contact");
            var response = await dynamics365Client.SendRequest(endpoint, HttpMethod.Post, data).ConfigureAwait(false);
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


        public IEnumerable<ContactInfo> GetContactsWithScore()
        {
            var minimumScore = ValidationHelper.GetInteger(settingsService[Dynamics365Constants.SETTINGS_MINSCORE], 0);
            return ContactInfo.Provider.Get()
                .WhereIn(
                    nameof(ContactInfo.ContactID),
                    ScoreContactRuleInfoProvider.GetContactsWithScore(minimumScore).AsSingleColumn(nameof(ContactInfo.ContactID))
                )
                .TypedResult
                .ToContactList();
        }


        public IEnumerable<ContactInfo> GetSynchronizedContacts()
        {
            return ContactInfo.Provider.Get()
                .WhereNotNull(Dynamics365Constants.CUSTOMFIELDS_LINKEDID)
                .TypedResult
                .ToContactList();
        }


        public async Task<SynchronizationResult> SynchronizeContacts(IEnumerable<ContactInfo> contacts)
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
                var entity = dynamics365EntityMapper.MapEntity("contact", mappingDefinition, contact);
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
            var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_PATCH, "contact", dynamicsId);

            return await dynamics365Client.SendRequest(endpoint, new HttpMethod("PATCH"), data).ConfigureAwait(false);
        }
    }
}