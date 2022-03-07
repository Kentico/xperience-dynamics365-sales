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


        public void CreateContact(ContactInfo contact, MappingModel mapping, SynchronizationResult currentResults)
        {
            try
            {
                var entity = dynamics365EntityMapper.MapEntity(Dynamics365Constants.ENTITY_CONTACT, mapping, contact);
                var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_GET_POST, Dynamics365Constants.ENTITY_CONTACT);
                var response = dynamics365Client.SendRequest(endpoint, HttpMethod.Post, entity);
                var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    var createdContact = JObject.Parse(responseContent);
                    var dynamicsId = createdContact.Value<string>("contactid");
                    if (!String.IsNullOrEmpty(dynamicsId))
                    {
                        // Success, link contact
                        contact.SetValue(Dynamics365Constants.CUSTOMFIELDS_SYNCEDON, DateTime.Now);
                        contact.SetValue(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, dynamicsId);
                        contact.Update();

                        if (dynamics365ActivitySync.SynchronizationEnabled())
                        {
                            SynchronizeActivities(contact, dynamicsId);
                        }

                        currentResults.SynchronizedObjectCount++;
                    }
                    else
                    {
                        // Success, but can't link contact
                        var message = $"While synchronizing the contact '{contact.ContactDescriptiveName}', the request was successful, but the Dynamics 365 ID could not be retrieved."
                            + " Please delete the contact in Dynamics 365 and contact the developer.";
                        eventLogService.LogError(nameof(DefaultDynamics365ContactSync), nameof(CreateContact), message);

                        var linkError = "Unable to retrieve Dynamics 365 contact ID. Please check the Event Log.";
                        currentResults.UnsynchronizedObjectIdentifiers.Add($"{contact.ContactDescriptiveName} ({contact.ContactGUID})");
                        if (!currentResults.SynchronizationErrors.Contains(linkError))
                        {
                            currentResults.SynchronizationErrors.Add(linkError);
                        }

                        currentResults.SynchronizedObjectCount++;
                    }
                }
                else
                {
                    // Failure
                    currentResults.UnsynchronizedObjectIdentifiers.Add($"{contact.ContactDescriptiveName} ({contact.ContactGUID})");
                    if (!currentResults.SynchronizationErrors.Contains(responseContent))
                    {
                        currentResults.SynchronizationErrors.Add(responseContent);
                    }
                }
            }
            catch (Exception ex)
            {
                currentResults.UnsynchronizedObjectIdentifiers.Add($"{contact.ContactDescriptiveName} ({contact.ContactGUID})");
                if (!currentResults.SynchronizationErrors.Contains(ex.Message))
                {
                    currentResults.SynchronizationErrors.Add(ex.Message);
                }
            }
        }


        public IEnumerable<ContactInfo> GetContactsWithScore()
        {
            var minimumScore = ValidationHelper.GetInteger(settingsService[Dynamics365Constants.SETTINGS_MINSCORE], 0);
            return ContactInfo.Provider.Get()
                .WhereNull(Dynamics365Constants.CUSTOMFIELDS_LINKEDID)
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
            var synchronizationResult = new SynchronizationResult();
            var mappingDefinition = settingsService[Dynamics365Constants.SETTING_FIELDMAPPING];
            if (String.IsNullOrEmpty(mappingDefinition))
            {
                synchronizationResult.SynchronizationErrors.Add("Unable to load contact field mapping. Please check the settings.");
                synchronizationResult.UnsynchronizedObjectIdentifiers.AddRange(contacts.Select(c => $"{c.ContactDescriptiveName} ({c.ContactGUID})"));
                return synchronizationResult;
            }

            var mapping = JsonConvert.DeserializeObject<MappingModel>(mappingDefinition);
            foreach (var contact in contacts)
            {
                var dynamicsId = contact.GetStringValue(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, String.Empty);
                if (String.IsNullOrEmpty(dynamicsId))
                {
                    CreateContact(contact, mapping, synchronizationResult);
                }
                else
                {
                    UpdateContact(contact, dynamicsId, mapping, synchronizationResult);
                }
            }

            return synchronizationResult;
        }


        public void UpdateContact(ContactInfo contact, string dynamicsId, MappingModel mapping, SynchronizationResult currentResults)
        {
            try
            {
                var entity = dynamics365EntityMapper.MapPartialEntity(Dynamics365Constants.ENTITY_CONTACT, mapping, dynamicsId, contact);
                if (entity == null)
                {
                    currentResults.UnsynchronizedObjectIdentifiers.Add($"{contact.ContactDescriptiveName} ({contact.ContactGUID})");
                    var mappingError = "Unable to map entity. Please check the Event Log.";
                    if (!currentResults.SynchronizationErrors.Contains(mappingError))
                    {
                        currentResults.SynchronizationErrors.Add(mappingError);
                    }

                    return;
                }

                if (entity.Count == 0)
                {
                    // No changes to update
                    return;
                }

                var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_PATCH_GETSINGLE, Dynamics365Constants.ENTITY_CONTACT, dynamicsId);
                var response = dynamics365Client.SendRequest(endpoint, new HttpMethod("PATCH"), entity);
                if (response.IsSuccessStatusCode)
                {
                    currentResults.SynchronizedObjectCount++;
                }
                else
                {
                    currentResults.UnsynchronizedObjectIdentifiers.Add($"{contact.ContactDescriptiveName} ({contact.ContactGUID})");
                    var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    if (!currentResults.SynchronizationErrors.Contains(responseContent))
                    {
                        currentResults.SynchronizationErrors.Add(responseContent);
                    }
                }
            }
            catch (Exception ex)
            {
                currentResults.UnsynchronizedObjectIdentifiers.Add($"{contact.ContactDescriptiveName} ({contact.ContactGUID})");
                if (!currentResults.SynchronizationErrors.Contains(ex.Message))
                {
                    currentResults.SynchronizationErrors.Add(ex.Message);
                }
            }
        }


        private void SynchronizeActivities(ContactInfo contact, string dynamicsId)
        {
            var activities = ActivityInfo.Provider.Get()
                    .WhereEquals(nameof(ActivityInfo.ActivityContactID), contact.ContactID)
                    .TypedResult
                    .ToList();

            var result = dynamics365ActivitySync.SynchronizeActivities(dynamicsId, activities);
            if (result.SynchronizationErrors.Count > 0)
            {
                var message = $"The following errors occurred during synchronization of contact '{contact.ContactDescriptiveName}' activities:<br/><br/>{String.Join("<br/>", result.SynchronizationErrors)}"
                        + $"<br/><br/>As a result, the following activities were not synchronized:<br/><br/>{String.Join("<br/>", result.UnsynchronizedObjectIdentifiers)}";
                eventLogService.LogError(nameof(DefaultDynamics365ContactSync), nameof(SynchronizeActivities), message);
            }
        }
    }
}