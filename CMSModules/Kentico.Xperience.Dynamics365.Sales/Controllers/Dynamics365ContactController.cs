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
            var response = Request.CreateResponse();
            var dynamics365ContactSync = Service.Resolve<IDynamics365ContactSync>();
            if (!dynamics365ContactSync.SynchronizationEnabled())
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                response.ReasonPhrase = "Contact synchronization is disabled.";
                return response;
            }

            var user = BasicAuthenticate();
            if (user == null)
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                return response;
            }

            if (!user.IsAuthorizedPerObject(PermissionsEnum.Modify, ContactInfo.OBJECT_TYPE, SiteContext.CurrentSiteName))
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                response.ReasonPhrase = "The user provided by the Authorization header does not have permission to modify contacts.";
                return response;
            }

            var dynamicsId = entity.Value<string>("contactid");
            if (String.IsNullOrEmpty(dynamicsId))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ReasonPhrase = "The provided entity was not a contact.";
                return response;
            }

            var linkedContact = ContactInfo.Provider.Get()
                .WhereEquals(Dynamics365Constants.CUSTOMFIELDS_LINKEDID, dynamicsId)
                .TopN(1)
                .TypedResult
                .FirstOrDefault();

            if (linkedContact == null)
            {
                response.StatusCode = HttpStatusCode.NoContent;
                response.ReasonPhrase = $"No linked Xperience contact found for Dynamics 365 ID {dynamicsId}.";
                return response;
            }

            return UpdateContact(linkedContact, entity);
        }


        private HttpResponseMessage UpdateContact(ContactInfo contact, JObject entity)
        {
            var response = Request.CreateResponse();
            var settingsService = Service.Resolve<ISettingsService>();
            var dynamics365EntityMapper = Service.Resolve<IDynamics365EntityMapper>();
            var mappingDefinition = settingsService[Dynamics365Constants.SETTING_FIELDMAPPING];
            if (String.IsNullOrEmpty(mappingDefinition))
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ReasonPhrase = "Unable to load contact field mapping. Please check the settings.";
                return response;
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
                response.StatusCode = HttpStatusCode.OK;
            }
            else
            {
                response.StatusCode = HttpStatusCode.NoContent;
                response.ReasonPhrase = "No mapped fields have changed.";
            }

            return response;
        }


        private UserInfo BasicAuthenticate()
        {
            string username;
            string password;
            var authorizationHeader = Request.Headers.GetValues("Authorization").FirstOrDefault();
            if (String.IsNullOrEmpty(authorizationHeader))
            {
                return null;
            }

            if (SecurityHelper.TryParseBasicAuthorizationHeader(authorizationHeader, out username, out password))
            {
                var user = AuthenticationHelper.AuthenticateUser(username, password, SiteContext.CurrentSiteName, false, AuthenticationSourceEnum.ExternalOrAPI);
                if (user != null)
                {
                    return user;
                }
            }

            return null;
        }
    }
}