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

    <%--BACK BUTTON--%>
    <asp:Panel ID="pnlNavigation" runat="server" CssClass="dashboard-nav">

    <asp:Button ID="btnBack"
        runat="server"
        Text="← Back"
        CssClass="btn btn-secondary"
        OnClick="btnBack_Click"
        Visible="true" />

    </asp:Panel>

    <!-- SIMULATIONS PANEL -->
    <asp:Panel ID="pnlSimulations" runat="server" CssClass="dashboard-panel" Visible="false">
        <div class="section-title">Simulation Management</div>

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
        
         <h2>
            Simulation:<asp:Label ID="lblSimulationName" runat="server" CssClass="text-primary"></asp:Label>
         </h2>

        <h3>Manage Phases</h3>

        <asp:TextBox ID="txtPhaseTitle" runat="server" Placeholder="Phase Title"></asp:TextBox>
        <br />
        <asp:TextBox ID="txtObjective" runat="server" TextMode="MultiLine" Rows="3" Placeholder="Objective"></asp:TextBox>
        <br />

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
                <asp:ButtonField Text="View Scenes" CommandName="ViewScenes" ButtonType="Button" />

                <asp:TemplateField>
                    <ItemTemplate>

                        <asp:Button runat="server"
                            Text="◀"
                            CommandName="MoveLeft"
                            CommandArgument="<%# Container.DataItemIndex %>" />

                        <asp:Button runat="server"
                            Text="▶"
                            CommandName="MoveRight"
                            CommandArgument="<%# Container.DataItemIndex %>" />
                         
                        <asp:Button runat="server"
                            Text="Edit"
                            CssClass="btn btn-warning btn-sm"
                            CommandName="EditPhase"
                            CommandArgument="<%# Container.DataItemIndex %>" />

                        <asp:Button runat="server"
                            Text="Delete"
                            CommandName="DeletePhase"
                            CommandArgument="<%# Container.DataItemIndex %>" />

                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </asp:Panel>
    
        <%--Scenes Panel --%>
        <asp:Panel ID="pnlScenes" runat="server" Visible="false" CssClass="dashboard-panel">

        <h2>
            Phases:
            <asp:Label ID="lblScenePhaseTitle" runat="server" CssClass="text-primary"></asp:Label>
        </h2>

        <h3>Manage Scenes</h3>

        <!-- Create Scene -->
        <div class="create-box">

            <asp:TextBox ID="txtSceneTitle"
                runat="server"
                CssClass="form-control"
                Placeholder="Scene Title" />

           <asp:TextBox ID="txtVideoPath"
                runat="server"
                CssClass="form-control"
                Placeholder="MP4 Video Path (optional)" />

            <asp:Button ID="btnAddScene"
                runat="server"
                Text="Add Scene"
                CssClass="btn btn-success"
                OnClick="btnAddScene_Click" />

        </div>

        <br />

        <!-- Scene Grid -->
        <asp:GridView ID="gvScenes"
            runat="server"
            AutoGenerateColumns="false"
            DataKeyNames="SceneID,VideoPath"
            CssClass="table"
            OnRowCommand="gvScenes_RowCommand">

            <Columns>

                <asp:BoundField DataField="SceneTitle" HeaderText="Scene Title" />
                <asp:BoundField DataField="VideoPath" HeaderText="Video" />
                <asp:BoundField DataField="DisplayOrder" HeaderText="Order" />

                <%-- Reorder --%>
                <asp:TemplateField HeaderText="Order">
                    <ItemTemplate>

                        <asp:Button runat="server"
                            Text="◀"
                            CommandName="MoveLeftScene"
                            CommandArgument="<%# Container.DataItemIndex %>"
                            CssClass="btn btn-sm btn-light" />

                        <asp:Button runat="server"
                            Text="▶"
                            CommandName="MoveRightScene"
                            CommandArgument="<%# Container.DataItemIndex %>"
                            CssClass="btn btn-sm btn-light" />

                    </ItemTemplate>
                </asp:TemplateField>

                <%-- Manage --%>
                <asp:TemplateField>
                    <ItemTemplate>

                        <asp:Button runat="server"
                            Text="Edit"
                            CommandName="EditScene"
                            CommandArgument="<%# Container.DataItemIndex %>"
                            CssClass="btn btn-warning btn-sm" />

                        <asp:Button runat="server"
                            Text="Decision Point"
                            CommandName="ManageDecisions"
                            CommandArgument="<%# Container.DataItemIndex %>"
                            CssClass="btn btn-secondary btn-sm" />

                        <asp:Button runat="server"
                            Text="Dialogue"
                            CommandName="ManageDialogue"
                            CommandArgument="<%# Container.DataItemIndex %>"
                            CssClass="btn btn-info btn-sm" />

                        <asp:Button runat="server"
                            Text="Delete"
                            CommandName="DeleteScene"
                            CommandArgument="<%# Container.DataItemIndex %>"
                            CssClass="btn btn-danger btn-sm" />

                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </asp:Panel>

        <%-- Dialogue Panel --%>
        <asp:Panel ID="pnlDialogue" runat="server" Visible="false" CssClass="dashboard-panel">

            <h2>
            Scene:
            <asp:Label ID="lblSceneName" runat="server"></asp:Label>
            </h2>

        <h2>Scene Dialogue</h2>

        <asp:TextBox ID="txtSpeaker"
            runat="server"
            CssClass="form-control"
            Placeholder="Speaker"></asp:TextBox>

        <asp:TextBox ID="txtDialogue"
            runat="server"
            CssClass="form-control"
            TextMode="MultiLine"
            Rows="3"
            Placeholder="Dialogue text"></asp:TextBox>

        <asp:Button ID="btnAddDialogue"
            runat="server"
            Text="Add Dialogue"
            CssClass="btn btn-success"
            OnClick="btnAddDialogue_Click" />

        <br /><br />

        <asp:GridView ID="gvDialogue"
            runat="server"
            AutoGenerateColumns="false"
            DataKeyNames="DialogueID"
            CssClass="table"
            OnRowCommand="gvDialogue_RowCommand">

            <Columns>

                <asp:BoundField DataField="Speaker" HeaderText="Speaker" />

                <asp:BoundField DataField="Dialogue" HeaderText="Dialogue" />

                <asp:BoundField DataField="DisplayOrder" HeaderText="Order" />

                <asp:TemplateField HeaderText="Actions">
                    <ItemTemplate>

                        <asp:Button ID="btnLeft"
                            runat="server"
                            Text="⬅"
                            CommandName="MoveLeft"
                            CommandArgument="<%# Container.DataItemIndex %>" />
                            

                        <asp:Button ID="btnRight"
                            runat="server"
                            Text="➡"
                            CommandName="MoveRight"
                            CommandArgument="<%# Container.DataItemIndex %>" />
                            
                        

                        <asp:Button ID="btnEdit"
                            runat="server"
                            Text="Edit"
                            CommandName="EditDialogue"
                            CommandArgument="<%# Container.DataItemIndex %>" />
                            

                        <asp:Button ID="btnDelete"
                            runat="server"
                            Text="Delete"
                            CommandName="DeleteDialogue"
                            CommandArgument="<%# Container.DataItemIndex %>"
                            OnClientClick="return confirm('Delete this dialogue line?');" />
                            

                    </ItemTemplate>
                </asp:TemplateField>

            </Columns>

        </asp:GridView>

    </asp:Panel>

        <%-- DecisionPoint Panel --%>
        <asp:Panel ID="pnlDecisionPoints" runat="server" Visible="false">
            <h2>
            Scene:
            <asp:Label ID="lblSceneNameDecision" runat="server"></asp:Label>
            </h2>
        <h3>Decision Points</h3>

        <asp:GridView ID="gvDecisionPoints" runat="server"
            AutoGenerateColumns="false"
            DataKeyNames="DecisionPointID"
            CssClass="table"
            OnRowCommand="gvDecisionPoints_RowCommand">

            <Columns>

            <asp:BoundField DataField="DecisionPrompt" HeaderText="Decision Prompt" />

            <asp:TemplateField HeaderText="Actions">
                <ItemTemplate>

                    <asp:LinkButton runat="server"
                        Text="Options"
                        CommandName="ManageOptions"
                        CommandArgument="<%# Container.DataItemIndex %>" />

                    |

                    <asp:LinkButton runat="server"
                        Text="Edit"
                        CommandName="EditDecision"
                        CommandArgument="<%# Container.DataItemIndex %>" />

                    |

                    <asp:LinkButton runat="server"
                        Text="Delete"
                        CommandName="DeleteDecision"
                        CommandArgument="<%# Container.DataItemIndex %>"
                        OnClientClick="return confirm('Delete decision point?');" />

                </ItemTemplate>
            </asp:TemplateField>

            </Columns>

        </asp:GridView>

        <br />

        <asp:TextBox ID="txtDecisionPrompt" runat="server" Width="400px"></asp:TextBox>

        <asp:Button ID="btnAddDecisionPoint"
            runat="server"
            Text="Add Decision Point"
            OnClick="btnAddDecisionPoint_Click" />

     <%-- Options Panel--%>
     <asp:Panel ID="pnlOptions" runat="server" Visible="false">

     <h3>Options</h3>

     <asp:GridView ID="gvOptions" runat="server"
         AutoGenerateColumns="false"
         DataKeyNames="OptionID"
         CssClass="table"
         OnRowCommand="gvOptions_RowCommand">

          <Columns>

            <asp:BoundField DataField="OptionLabel" HeaderText="Label" />

            <asp:BoundField DataField="OptionText" HeaderText="Option Text" />

            <asp:TemplateField HeaderText="Actions">
                <ItemTemplate>

                    <asp:LinkButton runat="server"
                        Text="Effects"
                        CommandName="ManageEffects"
                        CommandArgument="<%# Container.DataItemIndex %>" />

                    |

                    <asp:LinkButton runat="server"
                        Text="Edit"
                        CommandName="EditOption"
                        CommandArgument="<%# Container.DataItemIndex %>" />

                    |

                    <asp:LinkButton runat="server"
                        Text="Delete"
                        CommandName="DeleteOption"
                        CommandArgument="<%# Container.DataItemIndex %>" />

                </ItemTemplate>
            </asp:TemplateField>

        </Columns>

     </asp:GridView>

     <br />

     <asp:TextBox ID="txtOptionLabel" runat="server" Width="50"></asp:TextBox>

     <asp:TextBox ID="txtOptionText" runat="server" Width="400"></asp:TextBox>

     <asp:Button ID="btnAddOption"
         runat="server"
         Text="Add Option"
         OnClick="btnAddOption_Click" />

    <%-- AttributeEffects Panel--%>
    <asp:Panel ID="pnlAttributeEffects" runat="server" Visible="false">

    <h3>Attribute Effects</h3>

    <asp:GridView ID="gvEffects" runat="server"
        OnRowCommand="gvEffects_RowCommand"
        DataKeyNames="EffectID"
        AutoGenerateColumns="false"
        CssClass="table">

        <Columns>

            <asp:BoundField DataField="AttributeName" HeaderText="Attribute" />

            <asp:BoundField DataField="EffectValue" HeaderText="Effect" />

            <asp:TemplateField>
                <ItemTemplate>

                    <asp:LinkButton runat="server"
                        Text="Edit"
                        CommandName="EditEffect"
                        CommandArgument="<%# Container.DataItemIndex %>" />

                    |

                    <asp:LinkButton runat="server"
                        Text="Delete"
                        CommandName="DeleteEffect"
                        CommandArgument="<%# Container.DataItemIndex %>" />

                </ItemTemplate>
            </asp:TemplateField>

        </Columns>

    </asp:GridView>

    <br />

    Attribute

    <asp:DropDownList ID="ddlAttributes" runat="server"></asp:DropDownList>

    Effect

    <asp:DropDownList ID="ddlEffectValue" runat="server">
        <asp:ListItem Value="-3">-3</asp:ListItem>
        <asp:ListItem Value="-2">-2</asp:ListItem>
        <asp:ListItem Value="-1">-1</asp:ListItem>
        <asp:ListItem Value="0">0</asp:ListItem>
        <asp:ListItem Value="1">1</asp:ListItem>
        <asp:ListItem Value="2">2</asp:ListItem>
        <asp:ListItem Value="3">3</asp:ListItem>
    </asp:DropDownList>

    <asp:Button ID="btnAddEffect"
        runat="server"
        Text="Add Effect"
        OnClick="btnAddEffect_Click" />

    </asp:Panel>

    </asp:Panel>

    </asp:Panel>

        <%-- Attributes Panel --%>
        <asp:Panel ID="pnlAttributes" runat="server" Visible="false" CssClass="dashboard-panel">

        <h3>Simulation Attributes</h3>

        <asp:GridView ID="gvAttributes"
            runat="server"
            AutoGenerateColumns="false"
            DataKeyNames="AttributeID"
            CssClass="table"
            OnRowCommand="gvAttributes_RowCommand">

            <Columns>

                <asp:BoundField DataField="AttributeName" HeaderText="Attribute" />

                <asp:TemplateField>
                    <ItemTemplate>

                        <asp:LinkButton runat="server"
                            Text="Edit"
                            CommandName="EditAttribute"
                            CommandArgument="<%# Container.DataItemIndex %>" />

                        |

                        <asp:LinkButton runat="server"
                            Text="Delete"
                            CommandName="DeleteAttribute"
                            CommandArgument="<%# Container.DataItemIndex %>" />

                    </ItemTemplate>
                </asp:TemplateField>

            </Columns>

        </asp:GridView>

        <br />

        <asp:TextBox ID="txtAttributeName" runat="server" Width="250"></asp:TextBox>

        <asp:Button ID="btnAddAttribute"
            runat="server"
            Text="Add Attribute"
            OnClick="btnAddAttribute_Click" />

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

    <%--FOOTER--%>
    <div class="dashboard-footer, dashboard-header">

        <asp:Button ID="btnAttributes"
            runat="server"
            Text="Manage Attributes"
            CssClass="dashboard-card"
            OnClick="ShowAttributes" />

        <asp:Button ID="btnClusters"
            runat="server"
            Text="Manage Clusters"
            CssClass="dashboard-card"
            OnClick="ShowClusters" />

        <asp:Button ID="btnBands"
            runat="server"
            Text="Manage Band Definitions"
            CssClass="dashboard-card"
            OnClick="ShowBands" />

    </div>

</form>
</body>
</html>