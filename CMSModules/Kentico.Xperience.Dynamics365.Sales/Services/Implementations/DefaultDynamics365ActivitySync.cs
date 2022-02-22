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


        public DefaultDynamics365ActivitySync(IDynamics365Client dynamics365Client, IDynamics365EntityMapper dynamics365EntityMapper, ISettingsService settingsService)
        {
            this.dynamics365Client = dynamics365Client;
            this.dynamics365EntityMapper = dynamics365EntityMapper;
            this.settingsService = settingsService;
        }


        public async Task<HttpResponseMessage> CreateActivity(JObject data, string activityType)
        {
            var endpoint = String.Format(Dynamics365Constants.ENDPOINT_ENTITY_GET_POST, activityType);

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

            // Filter list to include only activities that exist in Dynamics 365
            var dynamicsActivityEntities = await GetAllActivities().ConfigureAwait(false);
            var dynamicsActivityNames = dynamicsActivityEntities.Select(entity => entity.LogicalName);
            var activitiesWithMatchingType = activities.Where(activity => dynamicsActivityNames.Contains(activity.ActivityType));
            foreach (var activity in activitiesWithMatchingType)
            {
                var entityName = activity.ActivityType;
                var entity = dynamics365EntityMapper.MapEntity(entityName, dynamicsId, activity);
                var response = await CreateActivity(entity, entityName).ConfigureAwait(false);

                // Handle response
                if (response.IsSuccessStatusCode)
                {
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
    }
}