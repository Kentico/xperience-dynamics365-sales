<%@ Control Language="C#" AutoEventWireup="True"  Codebehind="Mapping.ascx.cs" Inherits="CMSModules_Kentico_Xperience_Dynamics365_Sales_Controls_Mapping" %>

<div style="margin-top:0.5em" id="MessageControl" runat="server" enableviewstate="false" visible="false"></div>
<div id="ContainerControl" runat="server">
    <table style="margin-top:0.5em">
        <tbody>
            <tr>
                <td style="font-weight:bold;padding-bottom:1em;">Xperience</td>
                <td style="font-weight:bold;padding-left:2em;padding-bottom:1em;">Dynamics</td>
            </tr>
            <tr>
                <td style="padding-bottom:1em;">First name</td>
                <td style="padding-bottom:1em;padding-left:2em;">
                    <cms:CMSDropDownList ID="ContactFirstName" runat="server" />
                </td>
            </tr>
            <tr>
                <td style="padding-bottom:1em;">Middle name</td>
                <td style="padding-bottom:1em;padding-left:2em;">
                    <cms:CMSDropDownList ID="ContactMiddleName" runat="server" />
                </td>
            </tr>
            <tr>
                <td style="padding-bottom:1em;">Last name</td>
                <td style="padding-bottom:1em;padding-left:2em">
                    <cms:CMSDropDownList ID="ContactLastName" runat="server" />
                </td>
            </tr>
            <tr>
                <td style="padding-bottom:1em;">Email</td>
                <td style="padding-bottom:1em;padding-left:2em">
                    <cms:CMSDropDownList ID="ContactEmail" runat="server" />
                </td>
            </tr>
            <tr>
                <td style="padding-bottom:1em;">Mobile phone</td>
                <td style="padding-bottom:1em;padding-left:2em">
                    <cms:CMSDropDownList ID="ContactMobilePhone" runat="server" />
                </td>
            </tr>
            <tr>
                <td style="padding-bottom:1em;">Business phone</td>
                <td style="padding-bottom:1em;padding-left:2em">
                    <cms:CMSDropDownList ID="ContactBusinessPhone" runat="server" />
                </td>
            </tr>
            <tr>
                <td style="padding-bottom:1em;">Job title</td>
                <td style="padding-bottom:1em;padding-left:2em">
                    <cms:CMSDropDownList ID="ContactJobTitle" runat="server" />
                </td>
            </tr>
            <tr>
                <td style="padding-bottom:1em;">Address</td>
                <td style="padding-bottom:1em;padding-left:2em">
                    <cms:CMSDropDownList ID="ContactAddress1" runat="server" />
                </td>
            </tr>
            <tr>
                <td style="padding-bottom:1em;">City</td>
                <td style="padding-bottom:1em;padding-left:2em">
                    <cms:CMSDropDownList ID="ContactCity" runat="server" />
                </td>
            </tr>
            <tr>
                <td style="padding-bottom:1em;">ZIP code</td>
                <td style="padding-bottom:1em;padding-left:2em">
                    <cms:CMSDropDownList ID="ContactZIP" runat="server" />
                </td>
            </tr>
        </tbody>
    </table>
</div>