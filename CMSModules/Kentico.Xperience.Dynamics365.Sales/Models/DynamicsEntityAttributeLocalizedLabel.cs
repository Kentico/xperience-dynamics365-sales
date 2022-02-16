namespace Kentico.Xperience.Dynamics365.Sales.Models
{
    /// <summary>
    /// A localized label for a <see cref="DynamicsEntityAttributeModel"/>.
    /// </summary>
    public class DynamicsEntityAttributeLocalizedLabel
    {
        /// <summary>
        /// The localized label for the Entity attribute.
        /// </summary>
        public string Label
        {
            get;
            set;
        }


        /// <summary>
        /// The language code used to localize the <see cref="Label"/>.
        /// </summary>
        /// <remarks>See <see href="https://docs.microsoft.com/en-us/previous-versions/windows/embedded/ms912047(v=winembedded.10)"/>.</remarks>
        public int LanguageCode
        {
            get;
            set;
        }
    }
}