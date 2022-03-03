using CMS.Base.Web.UI;
using CMS.Core;
using CMS.Helpers;
using CMS.UIControls;

using Kentico.Xperience.Dynamics365.Sales.Constants;
using Kentico.Xperience.Dynamics365.Sales.Controls;
using Kentico.Xperience.Dynamics365.Sales.Models.Entity;
using Kentico.Xperience.Dynamics365.Sales.Models.Mapping;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;

namespace Kentico.Xperience.Dynamics365.Sales.Pages
{
    /// <summary>
    /// A modal dialog which displays editing controls to modify a <see cref="MappingModel"/>.
    /// </summary>
    public partial class MappingEditor : CMSModalPage
    {
        private string mSourceMappingHiddenFieldClientId;
        private string mSourceMappingPanelClientId;
        private List<MappingItemEditor> mMappingItemEditors;
        private IEnumerable<Dynamics365EntityAttributeModel> mEntityAttributes;
        private MappingModel mSourceMapping;


        /// <summary>
        /// The client ID of the hidden field which stores the serialized mapping from the
        /// window which opened this dialog.
        /// </summary>
        protected string SourceMappingHiddenFieldClientId
        {
            get
            {
                return mSourceMappingHiddenFieldClientId;
            }
        }


        /// <summary>
        /// The client ID of the panel which displays information on the window which opened
        /// this dialog.
        /// </summary>
        protected string SourceMappingPanelClientId
        {
            get
            {
                return mSourceMappingPanelClientId;
            }
        }


        /// <summary>
        /// The mapping that is being edited.
        /// </summary>
        protected MappingModel SourceMapping
        {
            get
            {
                return mSourceMapping;
            }
        }


        /// <summary>
        /// A list of the editing controls that have been dynamically added to the layout.
        /// </summary>
        protected List<MappingItemEditor> MappingItemEditors
        {
            get
            {
                if (mMappingItemEditors == null)
                {
                    mMappingItemEditors = new List<MappingItemEditor>();
                }

                return mMappingItemEditors;
            }
        }


        /// <summary>
        /// A list of the Entity's attributes that can be displayed in the mapping drop-down.
        /// </summary>
        private IEnumerable<Dynamics365EntityAttributeModel> EntityAttributes
        {
            get
            {
                if (mEntityAttributes == null)
                {
                    mEntityAttributes = Service.Resolve<IDynamics365Client>().GetEntityAttributes(Dynamics365Constants.ENTITY_CONTACT);
                }

                return mEntityAttributes;
            }
        }


        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            ScriptHelper.RegisterWOpenerScript(Page);
            ScriptHelper.RegisterJQuery(Page);

            PageTitle.TitleText = "Dynamics 365 contact mapping";
            Save += ConfirmButton_Click;

            RestoreParameters();
            MappingRepeater.ItemDataBound += new RepeaterItemEventHandler(MappingRepeater_ItemDataBound);
            MappingRepeater.DataSource = SourceMapping.Items;
            MappingRepeater.DataBind();
        }


        /// <summary>
        /// Initializes the <see cref="MappingItemEditor"/> control for each repeater row and adds the control to
        /// the <see cref="MappingItemEditor"/> list.
        /// </summary>
        protected void MappingRepeater_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            MappingItem item = e.Item.DataItem as MappingItem;
            MappingItemEditor control = e.Item.FindControl("MappingItemEditorControl") as MappingItemEditor;
            control.MappingItem = item;
            control.EntityAttributes = EntityAttributes;
            control.Initialize();
            MappingItemEditors.Add(control);
        }


        /// <summary>
        /// Dialog "Save and close" handler. Serializes the mapping data and stores it in a hidden field, and
        /// adds it to the window parameters.
        /// </summary>
        protected void ConfirmButton_Click(object sender, EventArgs e)
        {
            MappingHiddenField.Value = GetSerializedValue();
            string parametersIdentifier = QueryHelper.GetString("pid", null);
            Hashtable parameters = WindowHelper.GetItem(parametersIdentifier) as Hashtable;
            parameters["Mapping"] = MappingHiddenField.Value;
            WindowHelper.Add(parametersIdentifier, parameters);
        }


        private void RestoreParameters()
        {
            if (!QueryHelper.ValidateHash("hash"))
            {
                throw new InvalidOperationException("Invalid query hash.");
            }

            Hashtable parameters = WindowHelper.GetItem(QueryHelper.GetString("pid", null)) as Hashtable;
            if (parameters == null)
            {
                throw new InvalidOperationException("The dialog page parameters are missing, the session might have been lost.");
            }

            string content = ValidationHelper.GetString(parameters["Mapping"], String.Empty);
            mSourceMapping = CreateInitializedMapping(content);
            mSourceMappingHiddenFieldClientId = ValidationHelper.GetString(parameters["MappingHiddenFieldClientId"], null);
            mSourceMappingPanelClientId = ValidationHelper.GetString(parameters["MappingPanelClientId"], null);
        }


        /// <summary>
        /// Creates a new <see cref="MappingModel"/> and restores a mapping definition from the database
        /// (if it is not empty).
        /// </summary>
        /// <param name="mappingJson">The serialized mapping from the database, or an empty string.</param>
        /// <returns>An initialized mapping with valid Xperience fields and their mapped Dynamics 365 fields.</returns>
        private MappingModel CreateInitializedMapping(string mappingJson)
        {
            // Create a new mapping every time, in case fields were added/removed from ContactInfo
            var newMapping = new MappingModel();
            var contactFields = new ContactFormInfoProvider().GetContactFields();
            newMapping.Items.AddRange(contactFields.Select(field => new MappingItem(field)));

            // No previous mapping found in DB
            if (String.IsNullOrEmpty(mappingJson))
            {
                return newMapping;
            }
            
            // If the old mapping has values, move them to new mapping
            var existingMapping = JsonConvert.DeserializeObject<MappingModel>(mappingJson);
            foreach(var existingItem in existingMapping.Items)
            {
                if (!String.IsNullOrEmpty(existingItem.Dynamics365Field))
                {
                    var itemFromNewMapping = newMapping.Items.FirstOrDefault(i => i.XperienceFieldName == existingItem.XperienceFieldName);
                    if (itemFromNewMapping == null)
                    {
                        // The Xperience field doesn't exist in the new mapping- it was deleted
                        continue;
                    }

                    itemFromNewMapping.Dynamics365Field = existingItem.Dynamics365Field;
                }
            }

            return newMapping;
        }


        /// <summary>
        /// Iterates through all <see cref="MappingItemEditor"/> controls in <see cref="MappingItemEditors"/> and
        /// serializes the mapping into a string suitable for storing in the database.
        /// </summary>
        /// <returns></returns>
        private string GetSerializedValue()
        {
            var mapping = new MappingModel();
            foreach (var control in MappingItemEditors)
            {
                var mappingItem = control.MappingItem;
                var dynamicsAttributeName = control.SelectedValue;
                var attribute = EntityAttributes.FirstOrDefault(a => a.LogicalName == dynamicsAttributeName);
                if (attribute != null)
                {
                    mappingItem.DynamicsAttributeType = attribute.AttributeType;
                    mappingItem.DynamicsAttributeFormat = attribute.Format;
                }

                mappingItem.Dynamics365Field = dynamicsAttributeName;
                mapping.Items.Add(mappingItem);
            }

            return JsonConvert.SerializeObject(mapping);
        }
    }
}