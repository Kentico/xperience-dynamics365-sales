using CMS.Core;
using CMS.FormEngine.Web.UI;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Services;

using System;
using System.Web.UI.WebControls;

namespace Kentico.Xperience.Dynamics365.Sales.Controls
{
    /// <summary>
    /// A form control that displays a selection for an Entity attribute's acceptable values.
    /// </summary>
    public partial class OptionSetSelector : FormEngineUserControl
    {
        private int mValue = -1;


        /// <summary>
        /// The Entity to retrieve the attribute from.
        /// </summary>
        private string EntityName
        {
            get
            {
                return GetValue("EntityName", String.Empty);
            }
        }


        /// <summary>
        /// The Entity attribute to retrieve options from.
        /// </summary>
        private string AttributeName
        {
            get
            {
                return GetValue("AttributeName", String.Empty);
            }
        }


        public override object Value
        {
            get
            {
                return drpOptions.SelectedValue;
            }
            set
            {
                mValue = ValidationHelper.GetInteger(value, -1);
            }
        }


        protected void Page_Load(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(EntityName) || String.IsNullOrEmpty(AttributeName))
            {
                throw new InvalidOperationException("The required properties are not set for the form control.");
            }

            try
            {
                var endpoint = String.Format(Dynamics365Constants.ENDPOINT_OPTIONSET, EntityName, AttributeName);
                var entity = Service.Resolve<IDynamics365Client>().GetEntity(endpoint);
                if (entity == null)
                {
                    drpOptions.Enabled = false;
                    return;
                }

                drpOptions.Items.Add(new ListItem("(not set)", String.Empty));
                foreach (var option in entity.OptionSet.Options)
                {
                    drpOptions.Items.Add(new ListItem(option.Label.UserLocalizedLabel.Label, option.Value.ToString()));
                }

                if (mValue > -1)
                {
                    drpOptions.SelectedValue = mValue.ToString();
                }
            }
            catch (InvalidOperationException ex)
            {
                drpOptions.Visible = false;
                messageLabel.Visible = true;
                messageLabel.InnerHtml = ex.Message;
                messageLabel.Attributes.Add("class", "Red");
            }
        }
    }
}