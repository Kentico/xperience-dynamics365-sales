<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MappingEditor.aspx.cs" MasterPageFile="~/CMSMasterPages/UI/Dialogs/ModalDialogPage.master"
    Inherits="Kentico.Xperience.Dynamics365.Sales.Pages.MappingEditor" EnableEventValidation="false" Theme="Default" %>
<%@ Register TagPrefix="uc" TagName="MappingItemEditor" Src="~/CMSModules/Kentico.Xperience.Dynamics365.Sales/Controls/MappingItemEditor.ascx" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="plcContent" runat="Server" EnableViewState="false">
    <asp:HiddenField ID="MappingHiddenField" runat="server" EnableViewState="false" />
    <asp:Panel ID="MappingPanel" runat="server" EnableViewState="false" />
    <asp:Panel ID="MainPanel" runat="server" EnableViewState="false">
        <div class="form-horizontal">
            <asp:Repeater ID="MappingRepeater" runat="server" EnableViewState="false">
                <ItemTemplate>
                    <uc:MappingItemEditor ID="MappingItemEditorControl" runat="server" EnableViewState="false" />
                </ItemTemplate>
            </asp:Repeater>
        </div>
    </asp:Panel>
    <script type="text/javascript">
        $cmsj(document).ready(function () {
            var mappingField = document.getElementById('<%= MappingHiddenField.ClientID %>');
            var sourceMappingField = wopener.document.getElementById('<%= SourceMappingHiddenFieldClientId %>');
            if (mappingField != null && sourceMappingField != null && mappingField.value != null && mappingField.value != '') {
                $cmsj(sourceMappingField).val(mappingField.value);
                var panelElement = document.getElementById('<%= MappingPanel.ClientID %>');
                var sourcePanelElement = wopener.document.getElementById('<%= SourceMappingPanelClientId %>');
                if (panelElement != null && sourcePanelElement != null) {
                    var content = $cmsj(panelElement).html();
                    $cmsj(sourcePanelElement).html(content);
                }
                CloseDialog();
            }
        });
    </script>
</asp:Content>