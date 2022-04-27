namespace Kentico.Xperience.Dynamics365.Sales.Models.Entity
{
    /// <summary>
    /// Describes the structure of a Dynamics 365 Entity.
    /// </summary>
    public class Dynamics365EntityModel
    {
        /// <summary>
        /// The internal Entity name.
        /// </summary>
        public string LogicalName
        {
            get;
            set;
        }


        /// <summary>
        /// The option set for the Entity.
        /// </summary>
        public Dynamics365OptionSet OptionSet
        {
            get;
            set;
        }
    }
}