<%@ Control Language="C#" EnableViewState="true" AutoEventWireup="True" Codebehind="MappingButton.ascx.cs" Inherits="Kentico.Xperience.Dynamics365.Sales.Controls.MappingButton" %>

<cms:CMSUpdatePanel ID="MainUpdatePanel" runat="server">
<ContentTemplate>
    <div id="MessageLabel" style="margin-top:0.5em;margin-right:0.5em" runat="server" enableviewstate="false" visible="false"></div>
    <asp:HiddenField ID="MappingHiddenField" runat="server" EnableViewState="false" />
    <asp:Panel ID="MappingPanel" runat="server" EnableViewState="false" />
    <cms:LocalizedButton ID="EditMappingButton" runat="server" EnableViewState="false" ResourceString="general.edit" ButtonStyle="Default"></cms:LocalizedButton>
</ContentTemplate>
</cms:CMSUpdatePanel>

