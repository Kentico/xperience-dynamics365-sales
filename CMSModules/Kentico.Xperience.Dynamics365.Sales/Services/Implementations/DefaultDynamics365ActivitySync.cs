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
using System.Threading.Tasks;

[assembly: RegisterImplementation(typeof(IDynamics365ActivitySync), typeof(DefaultDynamics365ActivitySync), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    public class DefaultDynamics365ActivitySync : IDynamics365ActivitySync
    {
        private readonly IDynamics365Client dynamics365Client;
        private readonly IDynamics365EntityMapper dynamics365EntityMapper;
        private readonly ISettingsService settingsService;
        private readonly IProgressiveCache progressiveCache;


        public DefaultDynamics365ActivitySync(
            IDynamics365Client dynamics365Client,
            IDynamics365EntityMapper dynamics365EntityMapper,
            ISettingsService settingsService,
            IProgressiveCache progressiveCache)
        {
            this.dynamics365Client = dynamics365Client;
            this.dynamics365EntityMapper = dynamics365EntityMapper;
            this.settingsService = settingsService;
            this.progressiveCache = progressiveCache;
        }


        public async Task<HttpResponseMessage> CreateActivity(JObject data, string entityName)
        {
            var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_GET_POST, entityName);

            return await dynamics365Client.SendRequest(endpoint, HttpMethod.Post, data).ConfigureAwait(false);
        }


        public async Task<IEnumerable<Dynamics365EntityModel>> GetAllActivities()
        {
            var response = await dynamics365Client.SendRequest(Dynamics365Constants.ENDPOINT_GET_ACTIVITIES, HttpMethod.Get).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<Dynamics365EntityModel>();
            }

            var sourceJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var jObject = JObject.Parse(sourceJson);

            return JsonConvert.DeserializeObject<IEnumerable<Dynamics365EntityModel>>(jObject.Value<JArray>("value").ToString());
        }


        public async Task<SynchronizationResult> SynchronizeActivities(string dynamicsId, IEnumerable<ActivityInfo> activities)
        {
            var synchronizedActivities = 0;
            var unsyncedActivities = new List<string>();
            var synchronizationErrors = new List<string>();
            var dynamicsActivityEntities = await GetAllActivities().ConfigureAwait(false);
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
                var response = await CreateActivity(entity, entityToCreate).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    await MarkActivityComplete(entityToCreate, responseContent).ConfigureAwait(false);

                    synchronizedActivities++;
                }
                else
                {
                    unsyncedActivities.Add($"{activity.ActivityTitle} ({activity.ActivityID})");
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
                SynchronizedObjectCount = synchronizedActivities,
                UnsynchronizedObjectIdentifiers = unsyncedActivities
            };
        }


        public bool SynchronizationEnabled()
        {
            return ValidationHelper.GetBoolean(settingsService[Dynamics365Constants.SETTING_ACTIVITIESENABLED], false);
        }


        private async Task MarkActivityComplete(string entity, string responseContent)
        {
            var responseData = JObject.Parse(responseContent);
            var newId = responseData.Value<string>("activityid");
            if (String.IsNullOrEmpty(newId))
            {
                return;
            }

            var stateEndpoint = String.Format(Dynamics365Constants.ENDPOINT_STATECODES, entity);
            var entityStates = await dynamics365Client.GetEntity(stateEndpoint).ConfigureAwait(false);
            var completedState = entityStates.OptionSet.Options.FirstOrDefault(opt => opt.InvariantName == "Completed");
            if (completedState == null)
            {
                return;
            }

            var patchData = new JObject();
            patchData.Add("statecode", completedState.Value);
            patchData.Add("statuscode", completedState.DefaultStatus);
            
            var patchEndpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_PATCH_GETSINGLE, entity, newId);
            await dynamics365Client.SendRequest(patchEndpoint, new HttpMethod("PATCH"), patchData).ConfigureAwait(false);
        }
    }
}