namespace Kentico.Xperience.Dynamics365.Sales.Models.Entity
{
    /// <summary>
    /// An option for an Entity option set.
    /// </summary>
    public class Dynamics365OptionSetOption
    {
        /// <summary>
        /// The value (identifier) of the option.
        /// </summary>
        public int Value
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