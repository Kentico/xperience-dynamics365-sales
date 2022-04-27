using CMS.Base.Web.UI;

using Kentico.Xperience.Dynamics365.Sales.Models.Entity;
using Kentico.Xperience.Dynamics365.Sales.Models.Mapping;

using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

namespace Kentico.Xperience.Dynamics365.Sales.Controls
{
    /// <summary>
    /// A user control which displays options to map an Xperience field to a Dynamics 365 field.
    /// </summary>
    public partial class MappingItemEditor : AbstractUserControl
    {
        /// <summary>
        /// The mapping item being mapped by this control.
        /// </summary>
        public MappingItem MappingItem
        {
            get;
            set;
        }


        /// <summary>
        /// The name of the Dynamics 365 field that has been selected by the control.
        /// </summary>
        public string SelectedValue
        {
            get
            {
                return ddlFields.SelectedValue;
            }
        }


        /// <summary>
        /// A list of Dynamics 365 fields that can be mapped.
        /// </summary>
        public IEnumerable<Dynamics365EntityAttributeModel> EntityAttributes
        {
            get;
            set;
        }


        /// <summary>
        /// Populates the mapping dropdown.
        /// </summary>
        public void Initialize()
        {
            var listItems = ddlFields.Items;
            if (listItems.Count == 0)
            {
                listItems.Add(new ListItem("(not mapped)", String.Empty));
                foreach (var attr in EntityAttributes)
                {
                    var displayName = attr.LogicalName;
                    if (attr.DisplayName.UserLocalizedLabel != null && !String.IsNullOrEmpty(attr.DisplayName.UserLocalizedLabel.Label))
                    {
                        displayName = $"{attr.DisplayName.UserLocalizedLabel.Label} ({attr.LogicalName})";
                    }

                    listItems.Add(new ListItem(displayName, attr.LogicalName));
                }
            }

            if (!String.IsNullOrEmpty(MappingItem.Dynamics365Field))
            {
                ddlFields.SelectedValue = MappingItem.Dynamics365Field;
            }
        }
    }
}