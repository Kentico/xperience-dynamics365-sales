using System.Collections.Generic;

namespace Kentico.Xperience.Dynamics365.Sales.Models
{
    /// <summary>
    /// Describes the structure of a Dynamics Entity.
    /// </summary>
    public class DynamicsEntityModel
    {
        /// <summary>
        /// The Entity's attributes and their structure.
        /// </summary>
        public IEnumerable<DynamicsEntityAttributeModel> Value
        {
            get;
            set;
        }
    }
}