<%@ Page Title="" Language="C#" MasterPageFile="~/Default.Master" AutoEventWireup="true" CodeBehind="Step3.aspx.cs" Inherits="PasswordReset.SelfService.Step3" %>
<%@ MasterType VirtualPath="~/Default.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .center {
            width: 200px;
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="body" runat="server">
    <label class="label" runat="server">
        Nytt passord:
    </label>
    <div class="newline" />
    <asp:TextBox ID="txtNewPw" runat="server" Width="95%" textmode="Password"/>
    <div class="newline" />
    <label class="label" runat="server">
        Bekreft passord:
    </label>
    <div class="newline" />
    <asp:TextBox ID="txtConfirmPw" runat="server" Width="95%" textmode="Password"/>
    <div class="newline" />
    <div id="schoolUser" style="margin-top: 20px;" runat="server">
        <asp:CheckBox runat="server" ID="chkSchoolUserReset"/>
        <label class="label" runat="server">
            Reset skolepassord
        </label>
        <div class="newline"  />
    </div>
    <div class="newline" />
    <label class="label" runat="server">
        NB! Passord må være minimum 8 tegn og inneholde tall, store og små bokstaver
    </label>
    <asp:Button CssClass="fwrdBtn" runat="server" ID="btnStep3Forward" Text="Neste" OnClick="btnStep3Forward_Click"/>

    <asp:HiddenField ID="hdUsername" runat="server" />
    <asp:HiddenField ID="hdMobile" runat="server" />
    <asp:HiddenField ID="hdSessionId" runat="server" />
    <asp:HiddenField ID="hdHasSchoolUser" runat="server" />
    <asp:HiddenField ID="hdSMSCode" runat="server" />

</asp:Content>
