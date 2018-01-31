<%@ Page Title="" Language="C#" MasterPageFile="~/Default.Master" AutoEventWireup="true" CodeBehind="Step4.aspx.cs" Inherits="PasswordReset.SelfService.SuccessPage" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .center {
            width: 300px;
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="body" runat="server">
    <p class="success" runat="server">
        Ditt passord har blitt resatt.
    </p>
</asp:Content>
