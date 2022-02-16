using CMS;
using CMS.ContactManagement;
using CMS.Core;

using Kentico.Xperience.Dynamics365.Sales.Services;

using System.Collections.Generic;

[assembly: RegisterImplementation(typeof(IDynamics365ContactSync), typeof(DefaultDynamics365ContactSync), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    public class DefaultDynamics365ContactSync : IDynamics365ContactSync
    {
        private readonly IDynamics365Client dynamics365Client;


        public DefaultDynamics365ContactSync(IDynamics365Client dynamics365Client)
        {
            this.dynamics365Client = dynamics365Client;
        }


        public void SynchronizeContacts(List<ContactInfo> contacts)
        {

        }
    }
}