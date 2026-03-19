<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Authentication.aspx.cs" Inherits="Simulation_Based_Learning.Authentication" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">

<div class="scene">

    <div class="screen-wrap">

        <div class="screen">

            <h1>Simulation<br /><span>Tool</span></h1>

            <!-- LOGIN PANEL -->
            <asp:Panel ID="pnlLogin" runat="server">

                <asp:TextBox ID="txtLoginEmail" runat="server" placeholder="Email" /><br />

                <asp:TextBox ID="txtLoginPassword" runat="server" TextMode="Password" placeholder="Password" /><br />

                <asp:Button ID="btnLogin" runat="server" Text="Login" OnClick="btnLogin_Click" />

                <br />

                <asp:LinkButton ID="lnkShowRegister" runat="server" OnClick="lnkShowRegister_Click">
                    Create Account
                </asp:LinkButton>

                <br />

                <asp:Button ID="btnGuest" runat="server" Text="Continue as Guest" OnClick="btnGuest_Click" />

                <asp:Label ID="lblError" runat="server" ForeColor="Red" />

            </asp:Panel>


            <!-- REGISTER PANEL -->
            <asp:Panel ID="pnlRegister" runat="server" Visible="false">

                <asp:TextBox ID="txtUsername" runat="server" placeholder="Username" /><br />

                <asp:TextBox ID="txtEmail" runat="server" placeholder="Email" /><br />

                <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" placeholder="Password" /><br />

                <asp:Button ID="btnRegister" runat="server" Text="Register" OnClick="btnRegister_Click" />

                <br />

                <asp:LinkButton ID="lnkBackToLogin" runat="server" OnClick="lnkBackToLogin_Click">
                    Back to Login
                </asp:LinkButton>

            </asp:Panel>

        </div>

        <div class="noise"></div>

    </div>

</div>

</form>
</body>
</html>
