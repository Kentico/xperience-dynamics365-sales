using System.Collections.Generic;

namespace Kentico.Xperience.Dynamics365.Sales.Models.Entity
{
    /// <summary>
    /// An option set for a Dynamics 365 Entity.
    /// </summary>
    public class Dynamics365OptionSet
    {
        /// <summary>
        /// The available options for the option set.
        /// </summary>
        public IEnumerable<Dynamics365OptionSetOption> Options
        {
            get;
            set;
        }
    }
}