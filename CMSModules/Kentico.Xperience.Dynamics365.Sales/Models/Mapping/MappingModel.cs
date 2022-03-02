using System.Collections.Generic;

namespace Kentico.Xperience.Dynamics365.Sales.Models.Mapping
{
    /// <summary>
    /// Contains mapping information between Xperience object fields and Dynamics 365 fields.
    /// </summary>
    public class MappingModel
    {
        /// <summary>
        /// The mapped field definitions.
        /// </summary>
        public List<MappingItem> Items
        {
            get;
            set;
        } = new List<MappingItem>();
    }

}