using CMS;
using CMS.ContactManagement;
using CMS.Core;
using CMS.FormEngine;

using Kentico.Xperience.Dynamics365.Sales.Services;

using System.Collections.Generic;

[assembly: RegisterImplementation(typeof(IContactFieldProvider), typeof(DefaultContactFieldProvider), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Dynamics365.Sales.Services
{
    /// <summary>
    /// Default implementation of <see cref="IContactFieldProvider"/>.
    /// </summary>
    public class DefaultContactFieldProvider : IContactFieldProvider
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