using CMS.Core;
using CMS.FormEngine.Web.UI;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Services;

using System;
using System.Web.UI.WebControls;

namespace Kentico.Xperience.Dynamics365.Sales.Controls
{
    public partial class OptionSetSelector : FormEngineUserControl
    {
        private int mValue = -1;


        private string EntityName
        {
            get
            {
                return GetValue("EntityName", String.Empty);
            }
        }


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

            var endpoint = String.Format(Dynamics365Constants.ENDPOINT_OPTIONSET, EntityName, AttributeName);
            var entity = Service.Resolve<IDynamics365Client>().GetEntity(endpoint).ConfigureAwait(false).GetAwaiter().GetResult();

            drpOptions.Items.Add(new ListItem("(not set)", String.Empty));
            foreach(var option in entity.OptionSet.Options)
            {
                drpOptions.Items.Add(new ListItem(option.Label.UserLocalizedLabel.Label, option.Value.ToString()));
            }

            if (mValue > -1)
            {
                drpOptions.SelectedValue = mValue.ToString();
            }
        }
    }
}