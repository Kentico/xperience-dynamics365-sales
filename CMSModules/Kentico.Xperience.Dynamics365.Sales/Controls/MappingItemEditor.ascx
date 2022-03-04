<%@ Control Language="C#" EnableViewState="false" AutoEventWireup="true" CodeBehind="MappingItemEditor.ascx.cs" Inherits="Kentico.Xperience.Dynamics365.Sales.Controls.MappingItemEditor" %>

<div class="form-group" style="padding-bottom:0.4em">
    <div class="editing-form-label-cell" style="padding-top:0.4em;width:180px">
        <%= MappingItem.XperienceFieldCaption %>
    </div>
    <div class="editing-form-value-cell">
        <cms:CMSDropDownList CssClass="DropDownField" runat="server" ID="ddlFields" EnableViewState="false" />
    </div>
</div>