using CMS.ContactManagement;
using CMS.FormEngine;

using System.Collections.Generic;

namespace Kentico.Xperience.Dynamics365.Sales
{
    public class ContactFormInfoProvider
    {
        private HashSet<string> mExcludedFieldNames = new HashSet<string> {
            nameof(ContactInfo.ContactID),
            nameof(ContactInfo.ContactOwnerUserID),
            nameof(ContactInfo.ContactGUID),
            nameof(ContactInfo.ContactLastModified),
            nameof(ContactInfo.ContactCreated),
            nameof(ContactInfo.ContactBounces),
            nameof(ContactInfo.ContactCountryID),
            nameof(ContactInfo.ContactGender),
            nameof(ContactInfo.ContactStatusID),
            nameof(ContactInfo.ContactStateID),
            nameof(ContactInfo.ContactCompanyName),
            nameof(ContactInfo.ContactMonitored),
            nameof(ContactInfo.ContactCampaign)
        };


        /// <summary>
        /// Gets a list of the form fields from the OM.Contact class that should be included
        /// in the Dynamics 365 field mapping.
        /// </summary>
        public IEnumerable<FormFieldInfo> GetContactFields()
        {
            FormInfo form = FormHelper.GetFormInfo(ContactInfo.TYPEINFO.ObjectClassName, true);
            List<FormFieldInfo> fieldsToReturn = new List<FormFieldInfo>();
            foreach (FormFieldInfo field in form.GetFields(true, false, true))
            {
                if (!mExcludedFieldNames.Contains(field.Name))
                {
                    fieldsToReturn.Add(field);
                }
            }

            return fieldsToReturn;
        }
    }
}