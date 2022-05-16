using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;
using CMS.SiteProvider;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models.Mapping;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Kentico.Xperience.Dynamics365.Sales.Controllers
{
    /// <summary>
    /// A Web API controller which receives HTTP requests from Dynamics 365.
    /// </summary>
    public class Dynamics365ContactController : ApiController
    {
        /// <summary>
        /// A Web API endpoint which receives a Dynamics 365 contact entity and
        /// updates the linked Xperience contact if its field values are different
        /// from the Dynamics 365 mapped field values.
        /// </summary>
        /// <param name="entity">The Dynamics 365 contact entity.</param>
        /// <returns>A 200 response if the Xperience contact was updated, or another
        /// response indicating whether the request was processed or failed.</returns>
        [HttpPost]
        public HttpResponseMessage Update([FromBody] JObject entity)
        {
            var dynamics365ContactSync = Service.Resolve<IDynamics365ContactSync>();
            var eventLogService = Service.Resolve<IEventLogService>();
            if (!dynamics365ContactSync.SynchronizationEnabled())
            {
                eventLogService.LogError(nameof(Dynamics365ContactController), nameof(Update), "Contact synchronization is disabled.");
                return Request.CreateResponse(HttpStatusCode.Forbidden);
            }

            var user = BasicAuthenticate();
            if (user == null)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }

            if (!user.IsAuthorizedPerObject(PermissionsEnum.Modify, ContactInfo.OBJECT_TYPE, SiteContext.CurrentSiteName))
            {
                eventLogService.LogError(nameof(Dynamics365ContactController), nameof(Update), "The user provided by the Authorization header does not have permission to modify contacts.");
                return Request.CreateResponse(HttpStatusCode.Forbidden);
            }

            var dynamicsId = entity.Value<string>("contactid");
            if (String.IsNullOrEmpty(dynamicsId))
            {
                eventLogService.LogError(nameof(Dynamics365ContactController), nameof(Update), "The provided entity was not a contact.");
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

            var linkedContact = ContactInfo.Provider.Get()
                .WhereEquals(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, dynamicsId)
                .TopN(1)
                .TypedResult
                .FirstOrDefault();

            if (linkedContact == null)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent);
            }

            return UpdateContact(linkedContact, entity);
        }


        private HttpResponseMessage UpdateContact(ContactInfo contact, JObject entity)
        {
            var settingsService = Service.Resolve<ISettingsService>();
            var eventLogService = Service.Resolve<IEventLogService>();
            var dynamics365EntityMapper = Service.Resolve<IDynamics365EntityMapper>();
            var mappingDefinition = settingsService[Dynamics365Constants.SETTING_FIELDMAPPING];
            if (String.IsNullOrEmpty(mappingDefinition))
            {
                eventLogService.LogError(nameof(Dynamics365ContactController), nameof(UpdateContact), "Unable to load contact field mapping. Please check the settings.");
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }

            var mapping = JsonConvert.DeserializeObject<MappingModel>(mappingDefinition);
            var changedColumns = dynamics365EntityMapper.MapChangedColumns(mapping, contact, entity);
            if (changedColumns.Count > 0)
            {
                foreach (var column in changedColumns)
                {
                    contact.SetValue(column.Key, column.Value);
                }

                contact.Update();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
        }


        private UserInfo BasicAuthenticate()
        {
            string username;
            string password;
            IEnumerable<string> headerValues;
            var eventLogService = Service.Resolve<IEventLogService>();
            if (!Request.Headers.TryGetValues("Authorization", out headerValues))
            {
                eventLogService.LogError(nameof(Dynamics365ContactController), nameof(BasicAuthenticate), "Authorization header not present in request.");
                return null;
            }
            
            var authorizationHeader = headerValues.FirstOrDefault();
            if (String.IsNullOrEmpty(authorizationHeader))
            {
                eventLogService.LogError(nameof(Dynamics365ContactController), nameof(BasicAuthenticate), "Invalid Authorization header.");
                return null;
            }

            if (SecurityHelper.TryParseBasicAuthorizationHeader(authorizationHeader, out username, out password))
            {
                if (String.IsNullOrEmpty(password))
                {
                    eventLogService.LogError(nameof(Dynamics365ContactController), nameof(BasicAuthenticate), "Empty password is not allowed for incoming contact synchronization.");
                    return null;
                }

                return AuthenticationHelper.AuthenticateUser(username, password, SiteContext.CurrentSiteName, false, AuthenticationSourceEnum.ExternalOrAPI);
            }

            return null;
        }
    }
}