<%@ Page Title="" Language="C#" MasterPageFile="~/Default.Master" AutoEventWireup="true" CodeBehind="Step1.aspx.cs" Inherits="PasswordReset.SelfService.Step1" %>
<%@ MasterType VirtualPath="~/Default.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="body" runat="server">

    <label class="label" runat="server">
        Brukernavn:
    </label>
    <div class="newline" />
    <asp:TextBox ID="txtUsername" runat="server" />
    <div class="newline" />
    <label class="label" runat="server">
        Mobilnummer:
    </label>
    <div class="newline" />
    <asp:TextBox ID="txtMobileNumber" runat="server" />
    <div class="newline" />
    <asp:Button CssClass="fwrdBtn" runat="server" ID="btnForward" Text="Neste" OnClick="btnForward_Click"/>
    
    <asp:HiddenField ID="hdSessionId" runat="server"/>
    <asp:HiddenField ID="hdHasSchoolUser" runat="server" />

</asp:Content>
