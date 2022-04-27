using CMS;
using CMS.Activities;
using CMS.Core;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models;
using Kentico.Xperience.Dynamics365.Sales.Models.Entity;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

[assembly: RegisterImplementation(typeof(IDynamics365ActivitySync), typeof(DefaultDynamics365ActivitySync), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    /// <summary>
    /// The default implementation of <see cref="IDynamics365ActivitySync"/>.
    /// </summary>
    public class DefaultDynamics365ActivitySync : IDynamics365ActivitySync
    {
        private readonly IDynamics365Client dynamics365Client;
        private readonly IDynamics365EntityMapper dynamics365EntityMapper;
        private readonly ISettingsService settingsService;
        private readonly IProgressiveCache progressiveCache;
        private readonly IEventLogService eventLogService;


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDynamics365ActivitySync"/> class.
        /// </summary>
        public DefaultDynamics365ActivitySync(
            IDynamics365Client dynamics365Client,
            IDynamics365EntityMapper dynamics365EntityMapper,
            ISettingsService settingsService,
            IProgressiveCache progressiveCache,
            IEventLogService eventLogService)
        {
            this.dynamics365Client = dynamics365Client;
            this.dynamics365EntityMapper = dynamics365EntityMapper;
            this.settingsService = settingsService;
            this.progressiveCache = progressiveCache;
            this.eventLogService = eventLogService;
        }


        public HttpResponseMessage CreateActivity(JObject data, string entityName)
        {
            var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_GET_POST, entityName);

            return dynamics365Client.SendRequest(endpoint, HttpMethod.Post, data);
        }


        public IEnumerable<Dynamics365EntityModel> GetAllActivities()
        {
            return progressiveCache.Load(cacheSettings => {
                cacheSettings.CacheDependency = new CMSCacheDependency()
                {
                    CacheKeys = new string[]
                    {
                        $"cms.settingskey|{Dynamics365Constants.SETTING_CLIENTID.ToLower()}",
                        $"cms.settingskey|{Dynamics365Constants.SETTING_SECRET.ToLower()}",
                        $"cms.settingskey|{Dynamics365Constants.SETTING_TENANTID.ToLower()}"
                    }
                };

                try
                {
                    var response = dynamics365Client.SendRequest(Dynamics365Constants.ENDPOINT_GET_ACTIVITIES, HttpMethod.Get);
                    var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                    {
                        cacheSettings.Cached = false;
                        eventLogService.LogError(nameof(DefaultDynamics365ActivitySync), nameof(GetAllActivities), $"Error while retrieving Dynamics 365 activities: {responseContent}");
                        return Enumerable.Empty<Dynamics365EntityModel>();
                    }

                    var jObject = JObject.Parse(responseContent);
                    return JsonConvert.DeserializeObject<IEnumerable<Dynamics365EntityModel>>(jObject.Value<JArray>("value").ToString());
                }
                catch (Exception ex)
                {
                    cacheSettings.Cached = false;
                    eventLogService.LogError(nameof(DefaultDynamics365ActivitySync), nameof(GetAllActivities), $"Error while retrieving Dynamics 365 activities: {ex.Message}");
                    return Enumerable.Empty<Dynamics365EntityModel>();
                }
            }, new CacheSettings(TimeSpan.FromMinutes(Dynamics365Constants.CACHE_MINUTES).TotalMinutes, $"{nameof(DefaultDynamics365ActivitySync)}|{nameof(GetAllActivities)}"));
        }


        public SynchronizationResult SynchronizeActivities(string dynamicsId, IEnumerable<ActivityInfo> activities)
        {
            var synchronizationResult = new SynchronizationResult();
            var dynamicsActivityEntities = GetAllActivities();
            if (dynamicsActivityEntities.Count() == 0)
            {
                synchronizationResult.SynchronizationErrors.Add("Unable to retrieve activity types.");
                synchronizationResult.UnsynchronizedObjectIdentifiers.AddRange(activities.Select(activity => $"{activity.ActivityTitle} ({activity.ActivityID})"));
                return synchronizationResult;
            }

            var dynamicsActivityNames = dynamicsActivityEntities.Select(entity => entity.LogicalName);

            foreach (var activity in activities)
            {
                var entityToCreate = dynamics365EntityMapper.MapActivityType(activity.ActivityType);
                if (!dynamicsActivityNames.Contains(entityToCreate))
                {
                    // Entity doesn't exist in Dynamics 365
                    continue;
                }

                var entity = dynamics365EntityMapper.MapActivity(entityToCreate, dynamicsId, activity);
                try
                {
                    var response = CreateActivity(entity, entityToCreate);
                    if (response.IsSuccessStatusCode)
                    {
                        // Success
                        var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        MarkActivityComplete(entityToCreate, responseContent);

                        synchronizationResult.SynchronizedObjectCount++;
                    }
                    else
                    {
                        // Failure
                        synchronizationResult.UnsynchronizedObjectIdentifiers.Add($"{activity.ActivityTitle} ({activity.ActivityID})");
                        var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        if (!synchronizationResult.SynchronizationErrors.Contains(responseContent))
                        {
                            synchronizationResult.SynchronizationErrors.Add(responseContent);
                        }
                    }
                }
                catch (Exception ex)
                {
                    synchronizationResult.UnsynchronizedObjectIdentifiers.Add($"{activity.ActivityTitle} ({activity.ActivityID})");
                    if (!synchronizationResult.SynchronizationErrors.Contains(ex.Message))
                    {
                        synchronizationResult.SynchronizationErrors.Add(ex.Message);
                    }
                }
                
            }

            return synchronizationResult;
        }


        public bool SynchronizationEnabled()
        {
            return ValidationHelper.GetBoolean(settingsService[Dynamics365Constants.SETTING_ACTIVITIESENABLED], false);
        }


        private void MarkActivityComplete(string entity, string responseContent)
        {
            var responseData = JObject.Parse(responseContent);
            var newId = responseData.Value<string>("activityid");
            if (String.IsNullOrEmpty(newId))
            {
                return;
            }

            var stateEndpoint = String.Format(Dynamics365Constants.ENDPOINT_STATECODES, entity);
            var entityStates = dynamics365Client.GetEntity(stateEndpoint);
            if (entityStates == null)
            {
                return;
            }

            var completedState = entityStates.OptionSet.Options.FirstOrDefault(opt => opt.InvariantName == "Completed");
            if (completedState == null)
            {
                return;
            }

            var patchData = new JObject();
            patchData.Add("statecode", completedState.Value);
            patchData.Add("statuscode", completedState.DefaultStatus);
            
            var patchEndpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_PATCH_GETSINGLE, entity, newId);
            try
            {
                dynamics365Client.SendRequest(patchEndpoint, new HttpMethod("PATCH"), patchData);
            }
            catch (Exception ex)
            {
                eventLogService.LogError(nameof(DefaultDynamics365ActivitySync), nameof(MarkActivityComplete), ex.Message);
            }
        }
    }
}