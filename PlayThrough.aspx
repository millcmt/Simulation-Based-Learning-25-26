<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PlayThrough.aspx.cs" Inherits="Simulation_Based_Learning.PlayThrough" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<%-- Head --%>
<head runat="server">

    <%-- title and  stylesheet--%>
    <title>Simulation PlayThrough</title>
    <link rel="stylesheet" href="PlayThrough.css" />

</head>
<%-- Body --%>
<body>
    <%-- Main container for the simulation interface --%>
    <form id="form1" runat="server">
    <div class="simulation-container">

            <%--LOGOUT BUTTON--%>
            <asp:Button ID="btnLogout" runat="server" Text="Reset / Logout" OnClick="btnLogout_Click" CssClass="btn btn-danger" />

             <asp:Label
                ID="lblJoinError"
                runat="server"
                CssClass="error-text" />


            <!-- JOIN TEAM PANEL -->
            <asp:Panel ID="PanelJoinTeam" runat="server" CssClass="panel-card join-panel">
                 <asp:Button ID="Button3" runat="server" Text="Refresh" OnClick="btnRefresh_Click" />
                <h2>Join Simulation</h2>
                <p class="sub-text">Enter your team code to join a session</p>

                <div class="input-group">
                    <asp:TextBox
                        ID="txtJoinCode"
                        runat="server"
                        CssClass="input"
                        placeholder="e.g. A7K9P2">
                    </asp:TextBox>
                </div>

                <asp:Button
                    ID="btnJoinTeam"
                    runat="server"
                    CssClass="btn-primary"
                    Text="Join Team"
                    OnClick="btnJoinTeam_Click" />

                <hr style="margin: 20px 0;" />

                <!-- 🔥 HOST SECTION -->
                <h3>Create Team</h3>

                <asp:TextBox
                    ID="txtTeamName"
                    runat="server"
                    CssClass="input"
                    placeholder="Enter Team Name">
                </asp:TextBox>

                <asp:Button
                    ID="btnCreateTeam"
                    runat="server"
                    CssClass="btn-success"
                    Text="Create & Host"
                    OnClick="btnCreateTeam_Click" />

                
            </asp:Panel>

            <!-- LOBBY PANEL -->
            <asp:Panel ID="PanelLobby" runat="server" CssClass="panel-card lobby-panel" Visible="false">

                <div class="lobby-header">
                    <h2>Team Lobby</h2>
                    <span class="status-badge">Waiting for players...</span>

                </div>

                <div class="team-code-box">
                    <span>Team Code:</span>
                    <asp:Label ID="lblTeamCode" runat="server" CssClass="team-code" />
                </div>

                <div class="player-list">

                    <asp:Repeater ID="rptPlayers" runat="server">
                        <ItemTemplate>
                            <div class="player-card">
                                <span class="player-name"><%# Eval("Username") %></span>
                                <span class="player-status">Ready</span>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>

                </div>

                <asp:DropDownList ID="ddlSimulations" runat="server" AutoPostBack="true">
                </asp:DropDownList>

                <br />
                <br />

                <asp:Button ID="btnBack"
                    runat="server"
                    Text="← Back"
                    CssClass="btn btn-secondary"
                    OnClick="btnBack_Click"
                    Visible="true" />

                <asp:Button
                    ID="Button1"
                    runat="server"
                    CssClass="btn-success"
                    Text="Start Simulation"
                    OnClick="btnStartSimulation_Click" />


            </asp:Panel>

            <!-- CHARACTER SELECT PANEL -->
            <asp:Panel ID="PanelCharacterSelect" runat="server" CssClass="panel-card character-panel" Visible="false">

                <h2>Select Your Character</h2>
                <p class="sub-text">Choose wisely — each role is unique</p>

                <div class="character-grid">

                    <asp:Button ID="btnChar1" runat="server" CssClass="character-card" Text="Chief Executive Officer" OnClick="SelectCharacter" />
                    <asp:Button ID="btnChar2" runat="server" CssClass="character-card" Text="IT Director" OnClick="SelectCharacter" />
                    <asp:Button ID="btnChar3" runat="server" CssClass="character-card" Text="Human Resources Manager" OnClick="SelectCharacter" />
                    <asp:Button ID="btnChar4" runat="server" CssClass="character-card" Text="Digital Transformation Consultant" OnClick="SelectCharacter" />

                </div>

                <asp:Label ID="lblCharacterStatus" runat="server" CssClass="info-text" />
                <asp:Button ID="btnRefresh" runat="server" Text="Continue" OnClick="btnRefresh_Click" />


            </asp:Panel>

            <!-- SCENE PANEL -->
            <asp:Panel ID="PanelScene" runat="server" Visible="false">

                <h2>
                    <asp:Label ID="lblSceneTitle" runat="server" />
                </h2>

                <%--<video id="sceneVideo" runat="server" width="700" controls autoplay onended="onVideoEnd()">
                    <source id="sceneSource" runat="server" type="video/mp4" />
                </video>--%>


                <audio id="sceneAudio" runat="server" controls="controls" autoplay="autoplay" onended="onVideoEnd()">
                    <source id="sceneSource" runat="server" type="audio/mpeg" />
                </audio>


                <img id="sceneImagePath" runat="server" width="700" src="sceneImagePath" />

                <asp:Repeater ID="rptDialogue" runat="server">
                    <ItemTemplate>
                        <div class='<%# GetBubbleClass(Eval("Speaker").ToString()) %>'>
                            <strong><%# Eval("Speaker") %>:</strong><br />
                            <%# Eval("Dialogue") %>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>


                <script>
                    function onVideoEnd() {
                        __doPostBack('VideoEnded', '');
                    }
                </script>
                <br />
                <br />
                <asp:Button
                    ID="btnContinue"
                    runat="server"
                    Text="Continue"
                    OnClick="btnContinue_Click" />

            </asp:Panel>

            <!-- DECISION PANEL -->
            <asp:Panel ID="PanelDecision" runat="server" Visible="false">
                <asp:Button ID="Button2" runat="server" Text="Refresh" OnClick="btnRefresh_Click" />

                <asp:GridView ID="gvEffects" runat="server" AutoGenerateColumns="false">
                    <Columns>
                        <asp:BoundField DataField="AttributeName" HeaderText="Attribute" />
                        <asp:BoundField DataField="EffectValue" HeaderText="Effect" />
                    </Columns>
                </asp:GridView>

                <h2>
                    <asp:Label ID="lblDecisionQuestion" runat="server" />
                </h2>

                <asp:RadioButtonList
                    ID="rblOptions"
                    runat="server"
                    RepeatDirection="Vertical"
                    CssClass="decision-options" />

                <br />

                <asp:Button
                    ID="btnSubmitDecision"
                    runat="server"
                    Text="Submit Decision"
                    CssClass="btn btn-success"
                    OnClick="btnSubmitDecision_Click" />

                <asp:Button
                    ID="btnProceed"
                    runat="server"
                    Text="Proceed to Next Scene"
                    CssClass="btn btn-primary"
                    OnClick="btnProceed_Click" />

                <br />
                <br />

                <asp:Label ID="lblDecisionStatus" runat="server" ForeColor="Red" />

            </asp:Panel>

    <%-- Main container END --%>
    </div>
    </form>
    <%-- Main container END --%>
</body>
</html>




