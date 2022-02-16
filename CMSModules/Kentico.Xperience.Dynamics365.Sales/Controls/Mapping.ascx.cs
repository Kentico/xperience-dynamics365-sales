using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;

using CMS.Base.Web.UI;
using CMS.Core;
using CMS.FormEngine.Web.UI;
using CMS.Helpers;

using Kentico.Xperience.Dynamics365.Sales.Models;
using Kentico.Xperience.Dynamics365.Sales.Services;

using Newtonsoft.Json.Linq;

public partial class CMSModules_Kentico_Xperience_Dynamics365_Sales_Controls_Mapping : FormEngineUserControl
{
    private string mValue;
    private bool enabled = true;
    private IEnumerable<DynamicsEntityAttributeModel> entityAttributes;
    private readonly Regex whitespaceRegex = new Regex(@"\s+");


    public override object Value
    {
        get
        {
            mValue = GetValue();
            return mValue;
        }
        set
        {
            mValue = ValidationHelper.GetString(value, String.Empty);
        }
    }


    protected override void OnPreRender(EventArgs e)
    {
        base.OnPreRender(e);
        entityAttributes = LoadEntity();

        ContainerControl.Visible = enabled;
        if (!enabled)
        {
            return;
        }

        foreach (var control in ContainerControl.Controls)
        {
            var ddl = control as CMSDropDownList;
            if (ddl == null)
            {
                continue;
            }

            ddl.Items.Add(new ListItem("(not mapped)", String.Empty));
            foreach (var attr in entityAttributes)
            {
                var displayName = attr.LogicalName;
                if (attr.DisplayName.UserLocalizedLabel != null && !String.IsNullOrEmpty(attr.DisplayName.UserLocalizedLabel.Label))
                {
                    displayName = $"{attr.DisplayName.UserLocalizedLabel.Label} ({attr.LogicalName})";
                }

                ddl.Items.Add(new ListItem(displayName, attr.LogicalName));
            }
        }

        SetStoredValues();
    }


    private IEnumerable<DynamicsEntityAttributeModel> LoadEntity()
    {
        try
        {
            var entityModel = Service.Resolve<IDynamics365Client>().GetEntityModel("contact");
            if (entityModel == null)
            {
                return Enumerable.Empty<DynamicsEntityAttributeModel>();
            }

            return entityModel.Value;
        }
        catch (InvalidOperationException ex)
        {
            enabled = false;
            MessageControl.InnerHtml = ex.Message;
            MessageControl.Attributes.Add("class", "Red");
            MessageControl.Visible = true;
        }

        return Enumerable.Empty<DynamicsEntityAttributeModel>();
    }


    private string GetValue()
    {
        var data = new JObject();
        foreach (var control in ContainerControl.Controls)
        {
            var ddl = control as CMSDropDownList;
            if (ddl == null || String.IsNullOrEmpty(ddl.SelectedValue))
            {
                continue;
            }
            
            data[ddl.ID] = ddl.SelectedValue;
        }

        return whitespaceRegex.Replace(data.ToString(), String.Empty);
    }


    private void SetStoredValues()
    {
        if (String.IsNullOrEmpty(mValue))
        {
            return;
        }

        var data = JObject.Parse(mValue);
        foreach (var control in ContainerControl.Controls)
        {
            var ddl = control as CMSDropDownList;
            if (ddl == null)
            {
                continue;
            }

            var mappedField = data[ddl.ID]?.Value<string>();
            if (String.IsNullOrEmpty(mappedField))
            {
                continue;
            }

            ddl.SelectedValue = mappedField;
        }
    }
}