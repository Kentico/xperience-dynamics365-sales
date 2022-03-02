using CMS.ContactManagement;
using CMS.DataEngine;
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
        /// Creates a new instance of the contact form info suitable for mapping, and returns it.
        /// </summary>
        /// <returns>A new instance of the contact form info suitable for mapping.</returns>
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