using CMS;
using CMS.Activities;
using CMS.ContactManagement;
using CMS.Core;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models;
using Kentico.Xperience.Dynamics365.Sales.Models.Mapping;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

[assembly: RegisterImplementation(typeof(IDynamics365ContactSync), typeof(DefaultDynamics365ContactSync), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    /// <summary>
    /// The default implementation of <see cref="IDynamics365ContactSync"/>.
    /// </summary>
    public class DefaultDynamics365ContactSync : IDynamics365ContactSync
    {
        private readonly IDynamics365Client dynamics365Client;
        private readonly IDynamics365ActivitySync dynamics365ActivitySync;
        private readonly IDynamics365EntityMapper dynamics365EntityMapper;
        private readonly ISettingsService settingsService;
        private readonly IEventLogService eventLogService;


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDynamics365ContactSync"/> class.
        /// </summary>
        public DefaultDynamics365ContactSync(
            IDynamics365Client dynamics365Client,
            IDynamics365ActivitySync dynamics365ActivitySync,
            IDynamics365EntityMapper dynamics365EntityMapper,
            ISettingsService settingsService,
            IEventLogService eventLogService)
        {
            this.dynamics365Client = dynamics365Client;
            this.dynamics365ActivitySync = dynamics365ActivitySync;
            this.dynamics365EntityMapper = dynamics365EntityMapper;
            this.settingsService = settingsService;
            this.eventLogService = eventLogService;
        }


        public bool SynchronizationEnabled()
        {
            return ValidationHelper.GetBoolean(settingsService[Dynamics365Constants.SETTINGS_CONTACTSENABLED], false)
                && ValidationHelper.GetInteger(settingsService[Dynamics365Constants.SETTINGS_MINSCORE], 0) > 0; 
        }


        public HttpResponseMessage CreateContact(ContactInfo contact, JObject data)
        {
            var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_GET_POST, Dynamics365Constants.ENTITY_CONTACT);
            var response = dynamics365Client.SendRequest(endpoint, HttpMethod.Post, data);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                var createdContact = JObject.Parse(responseJson);
                var dynamicsId = createdContact.Value<string>("contactid");
                if (!String.IsNullOrEmpty(dynamicsId))
                {
                    contact.SetValue(Dynamics365Constants.CUSTOMFIELDS_SYNCEDON, DateTime.Now);
                    contact.SetValue(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, dynamicsId);
                    contact.Update();

                    SynchronizeActivities(contact, dynamicsId);
                }
                else
                {
                    var message = $"While synchronizing the contact '{contact.ContactDescriptiveName}', the request was successful, but the Dynamics 365 ID could not be retrieved."
                        + " Please delete the contact in Dynamics 365 and contact the developer.";
                    eventLogService.LogError(nameof(DefaultDynamics365ContactSync), nameof(CreateContact), message);
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


        public SynchronizationResult SynchronizeContacts(IEnumerable<ContactInfo> contacts)
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

            var mapping = JsonConvert.DeserializeObject<MappingModel>(mappingDefinition);
            var synchronizedContacts = 0;
            var unsyncedContacts = new List<string>();
            var synchronizationErrors = new List<string>();
            foreach (var contact in contacts)
            {
                bool doCreate;
                var dynamicsId = contact.GetStringValue(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, String.Empty);
                var entity = GetEntityForContact(dynamicsId, contact, mapping, out doCreate);
                HttpResponseMessage response;

                // Send request
                if (doCreate)
                {
                    if (entity.Count == 0)
                    {
                        unsyncedContacts.Add($"{contact.ContactDescriptiveName} ({contact.ContactGUID})");
                        continue;
                    }

                    response = CreateContact(contact, entity);
                }
                else
                {
                    if (entity.Count == 0)
                    {
                        // It isn't an error to have zero properties (contact is up-to-date)
                        continue;
                    }

                    if (entity == null)
                    {
                        unsyncedContacts.Add($"{contact.ContactDescriptiveName} ({contact.ContactGUID})");
                        synchronizationErrors.Add("Unable to map partial object.");
                        continue;
                    }

                    response = UpdateContact(dynamicsId, entity);
                }

                // Handle response
                if (response.IsSuccessStatusCode)
                {
                    synchronizedContacts++;
                }
                else
                {
                    unsyncedContacts.Add($"{contact.ContactDescriptiveName} ({contact.ContactGUID})");
                    var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
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


        public HttpResponseMessage UpdateContact(string dynamicsId, JObject data)
        {
            var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_PATCH_GETSINGLE, Dynamics365Constants.ENTITY_CONTACT, dynamicsId);

            return dynamics365Client.SendRequest(endpoint, new HttpMethod("PATCH"), data);
        }


        private void SynchronizeActivities(ContactInfo contact, string dynamicsId)
        {
            var activities = ActivityInfo.Provider.Get()
                    .WhereEquals(nameof(ActivityInfo.ActivityContactID), contact.ContactID)
                    .TypedResult
                    .ToList();

            var result = dynamics365ActivitySync.SynchronizeActivities(dynamicsId, activities);
            if (result.HasErrors)
            {
                var message = $"The following errors occurred during synchronization of contact '{contact.ContactDescriptiveName}' activities:<br/><br/>{String.Join("<br/>", result.SynchronizationErrors)}"
                        + $"<br/><br/>As a result, the following activities were not synchronized:<br/><br/>{String.Join("<br/>", result.UnsynchronizedObjectIdentifiers)}";
                eventLogService.LogError(nameof(DefaultDynamics365ContactSync), nameof(SynchronizeActivities), message);
            }
        }


        /// <summary>
        /// Gets an Entity for upserting to Dynamics 365. If the <paramref name="contact"/> has already synchronized,
        /// a partial Entity is created by checking for local values that differ from Xperience 365's values. If the contact
        /// was deleted in Dynamics 365, a full Entity is generated for creation as indicated by <paramref name="doCreate"/>.
        /// </summary>
        /// <param name="dynamicsId">The ID of the linked Dynamics 365 contact, or an empty string if the contact wasn't
        /// synchronized.</param>
        /// <param name="contact">The contact to generate the Entity for.</param>
        /// <param name="mapping">The mapping definition.</param>
        /// <param name="doCreate">Returns true if the contact should be created instead of updated.</param>
        /// <returns></returns>
        private JObject GetEntityForContact(string dynamicsId, ContactInfo contact, MappingModel mapping, out bool doCreate)
        {
            if (String.IsNullOrEmpty(dynamicsId))
            {
                doCreate = true;
                return dynamics365EntityMapper.MapEntity(Dynamics365Constants.ENTITY_CONTACT, mapping, contact);
            }
            else
            {
                // Ensure contact exists in Dynamics
                var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_PATCH_GETSINGLE, Dynamics365Constants.ENTITY_CONTACT, dynamicsId);
                var response = dynamics365Client.SendRequest(endpoint, HttpMethod.Get);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Contact was deleted in Dynamics, perform full create and re-link
                    doCreate = true;
                    return dynamics365EntityMapper.MapEntity(Dynamics365Constants.ENTITY_CONTACT, mapping, contact);
                }
                else
                {
                    doCreate = false;
                    return dynamics365EntityMapper.MapPartialEntity(Dynamics365Constants.ENTITY_CONTACT, mapping, dynamicsId, contact);
                }
            }
        }
    }
}