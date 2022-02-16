using CMS.ContactManagement;

using System.Collections.Generic;

namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    public interface IDynamics365ContactSync
    {
        void SynchronizeContacts(List<ContactInfo> contacts);
    }
}