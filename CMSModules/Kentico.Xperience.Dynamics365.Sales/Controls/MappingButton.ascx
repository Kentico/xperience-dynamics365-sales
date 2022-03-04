<%@ Control Language="C#" EnableViewState="true" AutoEventWireup="True" Codebehind="MappingButton.ascx.cs" Inherits="Kentico.Xperience.Dynamics365.Sales.Controls.MappingButton" %>
<%@ Import Namespace="Kentico.Xperience.Dynamics365.Sales.Models.Mapping" %>

<cms:CMSUpdatePanel ID="pnlUpdate" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hidMappingValue" runat="server" EnableViewState="false" />
        <cms:LocalizedButton ID="btnEditMapping" runat="server" EnableViewState="false" ResourceString="general.edit" ButtonStyle="Default"></cms:LocalizedButton>
        <asp:Panel ID="pnlMappingMessage" Visible="false" runat="server" EnableViewState="false">
            <div style="margin-top:1em">The following fields are currently mapped:</div>
            <table style="margin-top:0.5em">
                <tbody>
                    <tr>
                        <td style="font-weight:bold;padding-bottom:0.5em;">Xperience</td>
                        <td style="font-weight:bold;padding-left:2em;padding-bottom:0.5em;">Dynamics 365</td>
                    </tr>
                    <asp:Repeater runat="server" ID="repMapping">
                        <ItemTemplate>
                            <tr>
                                <td style="padding-bottom:0.5em;"><%# (Container.DataItem as MappingItem).XperienceFieldName %></td>
                                <td style="padding-left:2em;padding-bottom:0.5em;"><%# (Container.DataItem as MappingItem).Dynamics365Field %></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater> 
                </tbody>
            </table>
        </asp:Panel>
    </ContentTemplate>
</cms:CMSUpdatePanel>

