<%@ Page Title="" Language="C#" MasterPageFile="~/Default.Master" AutoEventWireup="true" CodeBehind="Step2.aspx.cs" Inherits="PasswordReset.SelfService.Step2" %>
<%@ MasterType VirtualPath="~/Default.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="body" runat="server">

    <label class="label" runat="server">
        Kode mottatt på sms:
    </label>
    <div class="newline" />
    <asp:TextBox ID="txtSMSCode" runat="server" />
    <div class="newline" />
    <asp:Button CssClass="fwrdBtn" runat="server" ID="btnForwardStep2" Text="Neste" OnClick="btnForwardStep2_Click"/>

    <asp:HiddenField ID="hdUsername" runat="server" />
    <asp:HiddenField ID="hdMobile" runat="server" />
    <asp:HiddenField ID="hdSessionId" runat="server" />
    <asp:HiddenField ID="hdHasSchoolUser" runat="server" />
</asp:Content>
