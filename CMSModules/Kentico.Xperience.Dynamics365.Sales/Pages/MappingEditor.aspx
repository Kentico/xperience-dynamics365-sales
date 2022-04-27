<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MappingEditor.aspx.cs" MasterPageFile="~/CMSMasterPages/UI/Dialogs/ModalDialogPage.master"
    Inherits="Kentico.Xperience.Dynamics365.Sales.Pages.MappingEditor" EnableEventValidation="false" Theme="Default" %>
<%@ Register TagPrefix="uc" TagName="MappingItemEditor" Src="~/CMSModules/Kentico.Xperience.Dynamics365.Sales/Controls/MappingItemEditor.ascx" %>

<asp:Content ContentPlaceHolderID="plcContent" runat="Server" EnableViewState="false">
    <asp:HiddenField ID="hidMappingValue" runat="server" EnableViewState="false" />
    <div id="lblError" class="Red" runat="server" enableviewstate="false" visible="false"></div>
    <asp:Panel ID="pnlMain" runat="server" EnableViewState="false">
        <div class="form-horizontal">
            <asp:Repeater ID="repMappings" runat="server" EnableViewState="false">
                <ItemTemplate>
                    <uc:MappingItemEditor ID="mappingItemEditorControl" runat="server" EnableViewState="false" />
                </ItemTemplate>
            </asp:Repeater>
        </div>
    </asp:Panel>
    <script type="text/javascript">
        $cmsj(document).ready(function () {
            var mappingField = document.getElementById('<%= hidMappingValue.ClientID %>');
            var sourceMappingField = wopener.document.getElementById('<%= SourceMappingHiddenFieldClientId %>');
            if (mappingField != null && sourceMappingField != null && mappingField.value != null && mappingField.value != '') {
                $cmsj(sourceMappingField).val(mappingField.value);
                CloseDialog();
            }
        });
    </script>
</asp:Content>