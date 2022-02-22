using CMS.ContactManagement;

using Kentico.Xperience.Dynamics365.Sales.Models;

using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    /// <summary>
    /// Contains methods for synchronizing Xperience contacts to Dynamics 365.
    /// </summary>
    public interface IDynamics365ContactSync
    {
        /// <summary>
        /// Creates a new contact in Dynamics 365 and saves the external ID to a custom
        /// Xperience contact field.
        /// </summary>
        /// <param name="contact">The contact to create in Dynamics 365.</param>
        /// <param name="data">An object containing the Dynamics 365 fields and the values
        /// from the Xperience contact.</param>
        /// <returns>The response from the Web API.</returns>
        Task<HttpResponseMessage> CreateContact(ContactInfo contact, JObject data);


        /// <summary>
        /// Gets all contacts who meet the scoring requirements.
        /// </summary>
        IEnumerable<ContactInfo> GetContactsWithScore();


        /// <summary>
        /// Gets all contacts who have been synchronized to Dynamics 365.
        /// </summary>
        IEnumerable<ContactInfo> GetSynchronizedContacts();


        /// <summary>
        /// Upserts the provided contacts to Dynamics 365.
        /// </summary>
        /// <param name="contacts">The contacts to be upserted.</param>
        /// <returns>The results of the synchronization process.</returns>
        Task<SynchronizationResult> SynchronizeContacts(IEnumerable<ContactInfo> contacts);


        /// <summary>
        /// Returns true if contacts should be synchronized according to the settings.
        /// </summary>
        bool SynchronizationEnabled();


        /// <summary>
        /// Updates an existing contact in Dynamics 365 using the provided <paramref name="dynamicsId"/>.
        /// </summary>
        /// <param name="dynamicsId">The identifier of the contact in Dynamics 365 to update.</param>
        /// <param name="data">An object containing the Dynamics 365 fields and the values
        /// from the Xperience contact.</param>
        /// <returns>The response from the Web API.</returns>
        Task<HttpResponseMessage> UpdateContact(string dynamicsId, JObject data);
    }
}