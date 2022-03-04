using CMS.FormEngine;

using System.Collections.Generic;

namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    public interface IContactFieldProvider
    {
        /// <summary>
        /// Gets a list of the form fields from the OM.Contact class that should be included
        /// in the Dynamics 365 field mapping.
        /// </summary>
        IEnumerable<FormFieldInfo> GetContactFields();
    }
}