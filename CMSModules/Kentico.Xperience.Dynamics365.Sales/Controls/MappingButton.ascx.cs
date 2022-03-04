using CMS.Base.Web.UI;
using CMS.Core;
using CMS.FormEngine.Web.UI;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models.Mapping;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json;

using System;
using System.Collections;
using System.Linq;
using System.Web.UI;

namespace Kentico.Xperience.Dynamics365.Sales.Controls
{
    /// <summary>
    /// A form control which opens a dialog for mapping Xperience contact fields to Dynamics 365
    /// contact fields as a <see cref="MappingModel"/>. The model is serialized and stored in a hidden
    /// field, which is then saved to a settings key.
    /// </summary>
    public partial class MappingButton : FormEngineUserControl, ICallbackEventHandler
    {
        public override object Value
        {
            get
            {
                return hidMappingValue.Value;
            }
            set
            {
                hidMappingValue.Value = value as string;
            }
        }


        /// <summary>
        /// The client ID of the hidden field which stores the serialized value;
        /// </summary>
        public override string ValueElementID
        {
            get
            {
                return hidMappingValue.ClientID;
            }
        }


        /// <summary>
        /// The GUID used to locate the window parameters.
        /// </summary>
        protected string ParametersId
        {
            get
            {
                string parametersId = ViewState["PID"] as string;
                if (String.IsNullOrEmpty(parametersId))
                {
                    parametersId = Guid.NewGuid().ToString("N");
                    ViewState["PID"] = parametersId;
                }

                return parametersId;
            }
        }


        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            ScriptHelper.RegisterDialogScript(Page);
            ScriptHelper.RegisterJQuery(Page);
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            string baseUrl = UrlResolver.ResolveUrl("~/CMSModules/Kentico.Xperience.Dynamics365.Sales/Pages/MappingEditor.aspx");
            string url = String.Format("{0}?pid={1}", baseUrl, ParametersId);
            string script = String.Format("function Dynamics_EditContactMapping (arg, context) {{ modalDialog('{0}', 'EditContactMapping', '900', '600', null); return false; }}", URLHelper.AddParameterToUrl(url, "hash", QueryHelper.GetHash(url)));
            ScriptHelper.RegisterClientScriptBlock(this, GetType(), "Dynamics_EditContactMapping", script, true);
        }


        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            EnsureConnection();
            DisplayCurrentMapping();
            btnEditMapping.OnClientClick = String.Format("{0}; return false;", Page.ClientScript.GetCallbackEventReference(this, null, "Dynamics_EditContactMapping", null));
        }


        /// <summary>
        /// Shows a message with the mapping information from the database.
        /// </summary>
        private void DisplayCurrentMapping()
        {
            var currentMapping = Value as string;
            if (String.IsNullOrEmpty(currentMapping))
            {
                return;
            }

            var mapping = JsonConvert.DeserializeObject<MappingModel>(currentMapping);
            var itemsWithMapping = mapping.Items.Where(item => !String.IsNullOrEmpty(item.Dynamics365Field));
            if (itemsWithMapping.Count() == 0)
            {
                return;
            }

            repMapping.DataSource = itemsWithMapping;
            repMapping.DataBind();
            pnlMappingMessage.Visible = true;
        }


        /// <summary>
        /// Tests the connection to Dynamics 365 and disables the mapping if needed.
        /// </summary>
        private void EnsureConnection()
        {
            var entityAttributes = Service.Resolve<IDynamics365Client>().GetEntityAttributes(Dynamics365Constants.ENTITY_CONTACT);
            if (entityAttributes.Count() == 0)
            {
                btnEditMapping.Enabled = false;
                btnEditMapping.ToolTip = "Unable to map fields, please check the Event Log.";
            }
        }


        private Hashtable CreateParameters()
        {
            Hashtable parameters = new Hashtable();
            parameters["Mapping"] = Value;
            parameters["MappingHiddenFieldClientId"] = hidMappingValue.ClientID;

            return parameters;
        }


        string ICallbackEventHandler.GetCallbackResult()
        {
            return null;
        }


        void ICallbackEventHandler.RaiseCallbackEvent(string eventArgument)
        {
            Hashtable parameters = WindowHelper.GetItem(ParametersId) as Hashtable;
            if (parameters == null)
            {
                parameters = CreateParameters();
                WindowHelper.Add(ParametersId, parameters);
            }
        }
    }
}