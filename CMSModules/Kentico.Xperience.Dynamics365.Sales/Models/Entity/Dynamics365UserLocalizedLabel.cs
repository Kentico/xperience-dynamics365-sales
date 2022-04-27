namespace Kentico.Xperience.Dynamics365.Sales.Models.Entity
{
    /// <summary>
    /// A localized label for an Entity attribute or option set option.
    /// </summary>
    public class Dynamics365UserLocalizedLabel
    {
        /// <summary>
        /// The localized label.
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