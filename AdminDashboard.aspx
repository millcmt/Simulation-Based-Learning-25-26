<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AdminDashboard.aspx.cs" Inherits="Simulation_Based_Learning.AdminDashboard" %>
<link rel="stylesheet" type="text/css" href="Main.css" />
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Admin Dashboard</title>
</head>

<body>
<form id="form1" runat="server">

<div class="dashboard-container">

    <!-- HEADER CARDS -->
    <div class="dashboard-header">

        <asp:Button ID="btnSimulations" runat="server"
            CssClass="dashboard-card"
            Text="Manage Simulations"
            OnClick="ShowSimulations" />

        <asp:Button ID="btnReports" runat="server"
            CssClass="dashboard-card"
            Text="View Reports"
            OnClick="ShowReports" />

        <asp:Button ID="btnAudit" runat="server"
            CssClass="dashboard-card"
            Text="Audit Activity"
            OnClick="ShowAudit" />

    </div>

    <!-- SIMULATIONS PANEL -->
    <asp:Panel ID="pnlSimulations" runat="server" CssClass="dashboard-panel" Visible="false">
        <div class="section-title">Simulation Management</div>

        <%--<asp:Button ID="btnCreateSimulation" runat="server"
            Text="Create Simulation"
            CssClass="btn-primary" />

        <br /><br />--%>

        <!-- Create Simulation Form -->
        <h3>Create Simulation</h3>

        <asp:TextBox ID="txtTitle" runat="server" CssClass="form-control" 
            Placeholder="Simulation Title"></asp:TextBox>

        <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-control">
            <asp:ListItem Text="Active" Value="Active"></asp:ListItem>
            <asp:ListItem Text="Completed" Value="Completed"></asp:ListItem>
        </asp:DropDownList>

        <asp:Button ID="btnCreate" runat="server"
            Text="Create Simulation"
            CssClass="btn btn-success"
            OnClick="btnCreate_Click" />

        <%--Simulations Grid--%>
        <asp:GridView ID="gvSimulations" runat="server"
            AutoGenerateColumns="false"
            GridLines="None"
            DataKeyNames="SimulationID"
            OnRowCommand="gvSimulations_RowCommand">
            <Columns>
                <asp:BoundField DataField="Title" HeaderText="Title" />
                <asp:BoundField DataField="Status" HeaderText="Status" />
                <asp:BoundField DataField="CreatedDate" HeaderText="Created Date" />
                <asp:TemplateField>
                    <ItemTemplate>
                        <asp:Button runat="server"
                            Text="Structure"
                            CommandName="StructureSim"
                            CommandArgument="<%# Container.DataItemIndex %>"
                            CssClass="btn btn-info btn-sm" />
                        <asp:Button runat="server"
                            Text="Edit"
                            CommandName="EditSim"
                            CommandArgument="<%# Container.DataItemIndex %>" 
                            CssClass="btn btn-warning btn-sm" />

                        <asp:Button runat="server"
                            Text="Delete"
                            CommandName="DeleteSim"
                            CommandArgument="<%# Container.DataItemIndex %>"
                            CssClass="btn btn-danger btn-sm" />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>

    </asp:Panel>

    <%--Phase Panel --%>
    <asp:Panel ID="pnlPhases" runat="server" CssClass="dashboard-panel" Visible="false">

    <h3>Manage Phases</h3>

    <asp:TextBox ID="txtPhaseTitle" runat="server" Placeholder="Phase Title"></asp:TextBox>
    <br />

    <asp:TextBox ID="txtObjective" runat="server" 
        TextMode="MultiLine" Rows="3" 
        Placeholder="Objective"></asp:TextBox>
    <br />

    <%--<asp:TextBox ID="txtDisplayOrder" runat="server" Placeholder="Display Order"></asp:TextBox>
    <br />--%>

    <asp:Button ID="btnAddPhase" runat="server"
        Text="Add Phase"
        OnClick="btnCreateTemplate_Click" />

    <br /><br />

    <asp:DropDownList ID="ddlPhaseTemplates" runat="server" />
        <asp:Button ID="btnAddExistingPhase" runat="server"
        Text="Add Existing Phase"
        OnClick="btnAddExistingPhase_Click" />

    <asp:GridView ID="gvPhases" runat="server"
        AutoGenerateColumns="false"
        DataKeyNames="SimulationPhaseID,PhaseTemplateID"
        OnRowCommand="gvPhases_RowCommand">

        <Columns>
            <asp:BoundField DataField="PhaseTitle" HeaderText="Title" />
            <asp:BoundField DataField="Objective" HeaderText="Objective" />
            <asp:BoundField DataField="DisplayOrder" HeaderText="Order" />
            <asp:ButtonField Text="View Scenes"
            CommandName="ViewScenes"
            ButtonType="Button" />

            <asp:TemplateField>
                <ItemTemplate>

                    <asp:Button runat="server"
                        Text="←"
                        CommandName="MoveLeft"
                        CommandArgument="<%# Container.DataItemIndex %>" />

                    <asp:Button runat="server"
                        Text="→"
                        CommandName="MoveRight"
                        CommandArgument="<%# Container.DataItemIndex %>" />

                    <asp:Button runat="server"
                        Text="Delete"
                        CommandName="DeletePhase"
                        CommandArgument="<%# Container.DataItemIndex %>" />

                </ItemTemplate>
            </asp:TemplateField>

        </Columns>
    </asp:GridView>

        <asp:Panel ID="pnlCreateScene" runat="server" CssClass="mb-3">

    <asp:TextBox ID="txtSceneTitle" runat="server"
        CssClass="form-control"
        Placeholder="Scene Title" />

    <asp:Button ID="btnAddScene" runat="server"
        Text="Add Scene"
        CssClass="btn btn-success mt-2"
        OnClick="btnAddScene_Click" />

</asp:Panel>
      <%--Scenes Panel --%>
        </asp:Panel>

            <asp:Panel ID="pnlScenes" runat="server" Visible="false">

            <h3>Scenes</h3>

            <asp:GridView ID="gvScenes" runat="server"
                AutoGenerateColumns="false"
                DataKeyNames="SceneID"
                CssClass="table">

                <Columns>
                    <asp:BoundField DataField="SceneTitle" HeaderText="Scene Title" />
                    <asp:BoundField DataField="SceneOrder" HeaderText="Order" />

                    <asp:ButtonField Text="Manage"
                        CommandName="ManageScene"
                        ButtonType="Button" />

                </Columns>

            </asp:GridView>

        </asp:Panel>



    <!-- REPORT PANEL -->
    <asp:Panel ID="pnlReports" runat="server" CssClass="dashboard-panel" Visible="false">
        <div class="section-title">Reports</div>
        <asp:GridView ID="gvReports" runat="server"></asp:GridView>
    </asp:Panel>

    <!-- AUDIT PANEL -->
    <asp:Panel ID="pnlAudit" runat="server" CssClass="dashboard-panel" Visible="false">
        <div class="section-title">Audit Activity</div>
        <asp:GridView ID="gvAudit" runat="server"></asp:GridView>
    </asp:Panel>

</div>

</form>
</body>
</html>