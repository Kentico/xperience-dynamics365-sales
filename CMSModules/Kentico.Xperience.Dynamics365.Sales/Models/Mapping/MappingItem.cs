using CMS.FormEngine;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Constants;

namespace Kentico.Xperience.Dynamics365.Sales.Models.Mapping
{
    /// <summary>
    /// Contains information regarding the mapping of a single Xperience object field and a
    /// Dynamics 365 field.
    /// </summary>
    public class MappingItem
    {
        /// <summary>
        /// The name of the Xperience field that is mapped.
        /// </summary>
        public string XperienceFieldName
        {
            get;
            set;
        }


        /// <summary>
        /// The human-friendly caption for the mapped Xperience field.
        /// </summary>
        public string XperienceFieldCaption
        {
            get;
            set;
        }


        /// <summary>
        /// The name of the Dynamics 365 field that is mapped.
        /// </summary>
        public string Dynamics365Field
        {
            get;
            set;
        }


        /// <summary>
        /// The <see cref="EntityAttributeType"/> of the mapped Dynamics 365 field.
        /// </summary>
        public string DynamicsAttributeType
        {
            get;
            set;
        }


        /// <summary>
        /// The value format of the Dynamics 365 field.
        /// </summary>
        public string DynamicsAttributeFormat
        {
            get;
            set;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="MappingItem"/> class for deserialization.
        /// </summary>
        public MappingItem()
        {

        }


        /// <summary>
        /// Initializes a new instance of the <see cref="MappingItem"/> class which contains the
        /// Xperience field information, but is not mapped to a Dynamics 365 field.
        /// </summary>
        public MappingItem(FormFieldInfo xperienceField)
        {
            XperienceFieldName = xperienceField.Name;
            XperienceFieldCaption = ResHelper.LocalizeString(xperienceField.Caption);
        }
    }

}