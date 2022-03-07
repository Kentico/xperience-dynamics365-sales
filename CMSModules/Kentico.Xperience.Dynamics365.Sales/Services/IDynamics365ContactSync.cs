using CMS.ContactManagement;

using Kentico.Xperience.Dynamics365.Sales.Models;
using Kentico.Xperience.Dynamics365.Sales.Models.Mapping;

using System.Collections.Generic;

namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    /// <summary>
    /// Contains methods for synchronizing Xperience contacts to Dynamics 365.
    /// </summary>
    public interface IDynamics365ContactSync
    {
        /// <summary>
        /// Creates a new contact in Dynamics 365 and saves the external ID and synchronized
        /// time to custom Xperience contact fields. Also synchronizes all past activities of
        /// the contact at the time of creation.
        /// </summary>
        /// <param name="contact">The contact to create in Dynamics 365.</param>
        /// <param name="mapping">The contact field mapping definiton.</param>
        /// <param name="currentResults">An object to track the results of the creation.</param>
        void CreateContact(ContactInfo contact, MappingModel mapping, SynchronizationResult currentResults);


        /// <summary>
        /// Gets all contacts who meet the scoring requirements, but have not been linked to a
        /// Dynamics 365 contact.
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
        SynchronizationResult SynchronizeContacts(IEnumerable<ContactInfo> contacts);


        /// <summary>
        /// Returns true if contacts should be synchronized according to the settings.
        /// </summary>
        bool SynchronizationEnabled();


        /// <summary>
        /// Updates an existing contact in Dynamics 365 using the provided <paramref name="dynamicsId"/>.
        /// </summary>
        /// <param name="contact">The Xperience contact being synchronized.</param>
        /// <param name="dynamicsId">The identifier of the contact in Dynamics 365 to update.</param>
        /// <param name="mapping">The contact field mapping definition.</param>
        /// <param name="currentResults">An object to track the results of the update.</param>
        void UpdateContact(ContactInfo contact, string dynamicsId, MappingModel mapping, SynchronizationResult currentResults);
    }
}