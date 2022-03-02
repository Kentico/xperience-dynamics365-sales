using CMS.Base.Web.UI;
using CMS.Core;
using CMS.FormEngine.Web.UI;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Models.Mapping;
using Kentico.Xperience.Dynamics365.Sales.Services;

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
                return MappingHiddenField.Value;
            }
            set
            {
                MappingHiddenField.Value = value as string;
            }
        }


        /// <summary>
        /// The client ID of the hidden field which stores the serialized value;
        /// </summary>
        public override string ValueElementID
        {
            get
            {
                return MappingHiddenField.ClientID;
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
            EditMappingButton.OnClientClick = String.Format("{0}; return false;", Page.ClientScript.GetCallbackEventReference(this, null, "Dynamics_EditContactMapping", null));
        }


        /// <summary>
        /// Tests the connection to Dynamics 365 and disables the mapping if needed.
        /// </summary>
        private void EnsureConnection()
        {
            var syncEnabled = Service.Resolve<IDynamics365ContactSync>().SynchronizationEnabled();
            if (!syncEnabled)
            {
                HandleError("Contact synchronization is not enabled.");
                return;
            }

            var entityAttributes = Service.Resolve<IDynamics365Client>().GetEntityAttributes(Dynamics365Constants.ENTITY_CONTACT);
            if (entityAttributes.Count() == 0)
            {
                HandleError("Couldn't connect to Dynamics 365. Please check the Event Log.");
            }
        }


        private void HandleError(string message)
        {
            Enabled = false;
            EditMappingButton.Visible = false;
            MessageLabel.InnerHtml = message;
            MessageLabel.Attributes.Add("class", "Red");
            MessageLabel.Visible = true;
        }


        private Hashtable CreateParameters()
        {
            Hashtable parameters = new Hashtable();
            parameters["Mapping"] = Value;
            parameters["MappingHiddenFieldClientId"] = MappingHiddenField.ClientID;
            parameters["MappingPanelClientId"] = MappingPanel.ClientID;

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