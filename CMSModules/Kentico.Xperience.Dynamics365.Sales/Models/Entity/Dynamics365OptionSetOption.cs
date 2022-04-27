namespace Kentico.Xperience.Dynamics365.Sales.Models.Entity
{
    /// <summary>
    /// An option for an Entity option set.
    /// </summary>
    public class Dynamics365OptionSetOption
    {
        /// <summary>
        /// The value of the option.
        /// </summary>
        public int Value
        {
            get;
            set;
        }


        /// <summary>
        /// Internal name of a state option that never changes.
        /// </summary>
        public string InvariantName
        {
            get;
            set;
        }


        /// <summary>
        /// The ID of a state option's default status code.
        /// </summary>
        public int DefaultStatus
        {
            get;
            set;
        }


        /// <summary>
        /// The human-friendly label for the option.
        /// </summary>
        public Dynamics365Label Label
        {
            get;
            set;
        }
    }
}