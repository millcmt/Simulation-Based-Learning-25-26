using Simulation_Based_Learning.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.EnterpriseServices;
using System.Linq;
using System.Security.Policy;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using static Simulation_Based_Learning.Repositories.PlaythroughRepository;

namespace Simulation_Based_Learning
{
    public partial class PlayThrough : System.Web.UI.Page
    {
        // Repositories
        private SimulationRepository sRepo = new SimulationRepository();
        private PlaythroughRepository pRepo = new PlaythroughRepository();

        // On page load, determine where the user is in the flow and show appropriate panel
        protected void Page_Load(object sender, EventArgs e)
        {
            // Must be logged in
            if (Session["UserID"] == null)
            {
                Response.Redirect("Authentication.aspx");
                return;
            }
            // Handle delayed refresh (outside IsPostBack)
            if (Request["__EVENTTARGET"] == "DelayedRedirect")
            {
                Response.Redirect(Request.RawUrl);
            }
            // Load simulations into dropdown on first load
            if (!IsPostBack)
            {
                // Load dropdown once
                ddlSimulations.DataSource = sRepo.GetAllSimulations();
                ddlSimulations.DataTextField = "Title";
                ddlSimulations.DataValueField = "SimulationID";
                ddlSimulations.DataBind();
            }
            // No team → Join screen
            if (Session["TeamID"] == null)
            {
                ShowPanel("Join");
                return;
            }
            // Has team but no session → Lobby
            int teamID = Convert.ToInt32(Session["TeamID"]);
            if (Session["SessionID"] == null)
            {
                // No session yet → stay in lobby
                ShowPanel("Lobby");
                LoadPlayers();
                ScriptManager.RegisterStartupScript(this, GetType(), "poll", "setTimeout(function(){ __doPostBack('DelayedRedirect', ''); }, 5000);", true);
                return;
            }
            // Sync game state to ensure correct panel is shown if player refreshes
            int sessionID = Convert.ToInt32(Session["SessionID"]);
            string currentPanel = ViewState["CurrentPanel"]?.ToString();
            if (currentPanel == "Decision")
            {
                LoadDecision(sessionID);
            }
            else if (currentPanel == "Scene")
            {
                LoadScene(sessionID);
            }
            else
            {
                LoadScene(sessionID); // default
            }  
        }

        // load players in lobby and display join code
        private void LoadPlayers()
        {
            if (Session["TeamID"] == null) return;

            int teamID = Convert.ToInt32(Session["TeamID"]);
            var data = pRepo.GetTeamLobbyData(teamID);

            // If joinCode failed → something wrong, stop
            if (data.joinCode == null)
            {
                lblJoinError.Text = "Failed to load lobby.";
                return;
            }

            rptPlayers.DataSource = data.players;
            rptPlayers.DataBind();
            lblTeamCode.Text = data.joinCode;
        }
        
        // load scene data and dialogue
        private void LoadScene(int sessionID)
        {
            // LOAD SCENE
            DataTable dt = pRepo.GetSceneBundle(sessionID);

            // If no scene data, something went wrong - stay on lobby
            if (dt.Rows.Count == 0) return;
            // Load first row for scene-level data (title, video, image)
            var first = dt.Rows[0];
            // Save current scene ID in session for sync checks
            Session["CurrentSceneID"] = first["SceneID"];
            
            lblSceneTitle.Text = first["SceneTitle"].ToString();
            sceneSource.Src = first["VideoPath"].ToString();
            sceneImagePath.Src = first["ImagePath"].ToString();
            
            // LOAD DIALOGUE
            var dialogue = dt.AsEnumerable()
                .Where(r => r["Dialogue"] != DBNull.Value)
                .Select(r => new
                {
                    Speaker = r["Speaker"].ToString(),
                    Dialogue = r["Dialogue"].ToString()
                }).Distinct().ToList();

            rptDialogue.DataSource = dialogue;
            rptDialogue.DataBind();

            //ShowPanel
            ShowPanel("Scene");
        }

        // load decision data from current scene 
        private void LoadDecision(int sessionID)
        {
            var dt = pRepo.GetSceneBundle(sessionID);

            // If no scene data, something went wrong - stay on lobby 
            if (dt.Rows.Count == 0) return;
            var first = dt.Rows[0];
            int sceneID = (int)first["SceneID"];

            // CHECK if decision exists (NO extra DB call)
            if (first["DecisionPointID"] != DBNull.Value)
            {
                int decisionID = (int)first["DecisionPointID"];

                // Save for submit later
                ViewState["DecisionID"] = decisionID;

                // Load decision prompt
                lblDecisionQuestion.Text = first["DecisionPrompt"].ToString();

                // Extract options from SAME dataset
                var options = dt.AsEnumerable()
                    .Where(r => r["OptionID"] != DBNull.Value)
                    .Select(r => new
                    {
                        OptionID = r["OptionID"],
                        OptionText = r["OptionText"]
                    })
                    .Distinct()
                    .ToList();

                if (!IsPostBack || rblOptions.Items.Count == 0)
                {
                    rblOptions.DataSource = options;
                    rblOptions.DataTextField = "OptionText";
                    rblOptions.DataValueField = "OptionID";
                    rblOptions.DataBind();
                }

                ShowPanel("Decision");
            }
        }

        










        

        // submit decision and check if all players have decided
        protected void btnSubmitDecision_Click(object sender, EventArgs e)
        {
            if (rblOptions.SelectedItem == null) { lblDecisionStatus.Text = "Please select an option before continuing."; return; }
             
            int sessionID = Convert.ToInt32(Session["SessionID"]);
            int userID = Convert.ToInt32(Session["UserID"]);
            int decisionID = Convert.ToInt32(ViewState["DecisionID"]);
            int optionID = int.Parse(rblOptions.SelectedValue);

            // This method both saves the decision and checks if all players have decided, returning the appropriate status
            var result = pRepo.SubmitDecisionAndAdvance(sessionID, userID, decisionID, optionID);

            if (result.Status == "WAITING")
            {
                lblDecisionStatus.Text = "Decision submitted. Waiting for others...";
                btnSubmitDecision.Enabled = false;
                ShowPanel("Decision");
                //        ScriptManager.RegisterStartupScript(this, GetType(), "poll",
                //"setTimeout(function(){ __doPostBack('DelayedRedirect',''); }, 3000);", true);
            }
            else if (result.Status == "RESOLVED")
            {
                lblDecisionStatus.Text = "Advancing...";
                rblOptions.Visible = false;
                lblDecisionQuestion.Visible = false;
                ShowTeamDecision(result);
                // show result, then reload to next scene
                ScriptManager.RegisterStartupScript(this, GetType(), "redirect",
                "setTimeout(function(){ window.location = window.location.href; }, 3000);", true);
                ////Response.Redirect(Request.RawUrl);
            }
            else if (result.Status == "COMPLETED")
            {
                lblDecisionStatus.Text = "Simulation completed.";
                rblOptions.Visible = false;
                lblDecisionQuestion.Visible = false;
                ShowTeamDecision(result);
                pRepo.UpdateSessionStatus(sessionID, "Completed");
                ScriptManager.RegisterStartupScript(this, GetType(), "redirect",
                    "setTimeout(function(){ window.location='Report.aspx'; }, 3000);", true);
            }
        }

        // In case a player tries to proceed without waiting for others, we check again if all players have decided before allowing them to continue
        protected void btnProceed_Click(object sender, EventArgs e)
        {
            int sessionID = Convert.ToInt32(Session["SessionID"]);
            int decisionID = pRepo.GetLatestDecisionID(sessionID);
            //int decisionID = Convert.ToInt32(ViewState["DecisionID"]);


            int remaining = pRepo.HaveAllPlayersDecided(sessionID, decisionID);
            string sessionStatus = pRepo.GetSessionStatus(sessionID);
            var result = pRepo.GetLatestTeamDecision(sessionID, decisionID);

            if (sessionStatus != "Completed")
            {
                if (remaining == 0)
                {
                    lblDecisionStatus.Text = "Advancing...";
                    
                    ShowTeamDecision(result);
                    btnProceed.Enabled = false;
                    rblOptions.Visible = false;
                    lblDecisionQuestion.Visible = false;
                    // show result, then reload to next scene
                    ScriptManager.RegisterStartupScript(this, GetType(), "redirect",
                    "setTimeout(function(){ window.location = window.location.href; }, 3000);", true);
                    ////Response.Redirect(Request.RawUrl);
                }
                else
                {
                    lblDecisionStatus.Text = $"Waiting for {remaining} player(s)...";
                }
            }
            else
            {
                lblDecisionStatus.Text = "Simulation completed.";
                btnProceed.Enabled = false;
                rblOptions.Visible = false;
                lblDecisionQuestion.Visible = false;
                ShowTeamDecision(result);
                ScriptManager.RegisterStartupScript(this, GetType(), "redirect",
                    "setTimeout(function(){ window.location='Report.aspx'; }, 3000);", true);
            }
        }

        // Host creates simulation, which creates session and moves them to character selection
        protected void btnStartSimulation_Click(object sender, EventArgs e)
        {
            int simulationID = int.Parse(ddlSimulations.SelectedValue);
            int teamID = Convert.ToInt32(Session["TeamID"]);
            int sessionID = pRepo.CreateSession(teamID, simulationID);
            pRepo.UpdateSessionStatus(sessionID, "Waiting"); // status before character selection
            Session["SessionID"] = sessionID;
            ShowPanel("Character"); // Move players to character selection
        }
        // create team logic - validate name, create team, add host as member, then show lobby
        protected void btnCreateTeam_Click(object sender, EventArgs e)
        {
            string teamName = txtTeamName.Text.Trim();

            if (string.IsNullOrEmpty(teamName))
            {
                lblJoinError.Text = "Enter a team name.";
                return;
            }
            lblJoinError.Text = $"Session in Session: {Session["SessionID"]}, user: {Session["UserID"]}";

            //Create team and add host as member
            int teamID = pRepo.CreateTeam(teamName);
            pRepo.AddMember(teamID, Convert.ToInt32(Session["UserID"]));

            Session["TeamID"] = teamID;
            lblJoinError.Text = "before. ShowPanel";
            ShowPanel("Lobby");
            lblJoinError.Text = "AFTER.";
            lblJoinError.Text = $"TeamID in Session: {Session["TeamID"]}, user: {Session["UserID"]}";
            LoadPlayers();
        }
        // join team logic - validate code, add player to team, then show lobby
        protected void btnJoinTeam_Click(object sender, EventArgs e)
        {
            // trim code ro get team the getting team by code and team ID then add member to team and show lobby
            string code = txtJoinCode.Text.Trim();
            int TeamID = pRepo.GetTeamByJoinCode(code);

            lblJoinError.Text = $"DEBUG → , teamID in Session: {TeamID}, user: {Session["UserID"]}";
            // Validate code
            if (TeamID == 0)
            {
                lblJoinError.Text = "Invalid team code.";
                return;
            }

            Session["TeamID"] = TeamID;

            
            

            // Add player to team then show lobby and load players
            if (Session["UserID"] == null)
            {
                lblJoinError.Text = "Session expired. Please log in again.";
                return;
            }

            lblJoinError.Text = "BEFORE.";
            pRepo.AddMember(TeamID, Convert.ToInt32(Session["UserID"]));
            ShowPanel("Lobby");
            lblJoinError.Text = "AFTER.";
            LoadPlayers();
        }
        // Character selection logic - save character choice, check if all players have selected, then move to scene
        protected void SelectCharacter(object sender, EventArgs e)
        {
            int teamID = Convert.ToInt32(Session["TeamID"]);
            int userID = Convert.ToInt32(Session["UserID"]);

            Button btn = (Button)sender;
            string character = btn.Text;

            // Ensure session exists
            int sessionID;
            if (Session["SessionID"] == null)
            {
                // Try to get existing session instead of creating
                sessionID = pRepo.GetActiveSessionID(teamID);
                if (sessionID == 0)
                {
                    lblCharacterStatus.Text = "Session not ready yet.";
                    return;
                }
                Session["SessionID"] = sessionID;
            }
            else
            {
                sessionID = Convert.ToInt32(Session["SessionID"]);
            }

            // Attempt to select the character
            bool success = pRepo.SelectCharacter(sessionID, userID, character);
            if (!success)
            {
                lblCharacterStatus.Text = $"Sorry, {character} is already taken.";
                btn.Enabled = false; // disable clicked button
                return;
            }

            lblCharacterStatus.Text = $"You have selected: {character}";

            // Disable already taken characters
            if (!IsPostBack)
            {
                DisableTakenCharacters(sessionID);
            }

            // Show the Character panel (post-selection)
            ShowPanel("Character");

            // Check if all players selected → auto move to Scene panel
            var status = pRepo.GetPlayerStatus(teamID, sessionID);

            int totalPlayers = status.totalPlayers;
            int selectedCount = status.selectedPlayers;
            
            if (totalPlayers > 0 && selectedCount >= totalPlayers)
            {
                // 🔥 Start simulation properly
                pRepo.UpdateSessionStatus(sessionID, "InProgress");
                // 🔥 Initialize FIRST scene
                pRepo.InitializeSessionProgress(sessionID);
                // 🔥 Reload page so everyone syncs
                Response.Redirect(Request.RawUrl);
            }
        }

        // Disable character buttons that have already been taken by other players
        private void DisableTakenCharacters(int sessionID)
        {
            PlaythroughRepository repo = new PlaythroughRepository();
            var taken = repo.GetTakenCharacters(sessionID); // returns List<string>

            // Assuming buttons are inside a div with class "character-grid"
            foreach (Control ctrl in PanelCharacterSelect.Controls)
            {
                if (ctrl is Panel divPanel)
                {
                    foreach (Control child in divPanel.Controls)
                    {
                        if (child is Button btn && taken.Contains(btn.Text))
                        {
                            btn.Enabled = false;
                            btn.BackColor = System.Drawing.Color.Gray;
                        }
                    }
                }
            }
        }





        





        // Utility to show/hide panels
        private void ShowPanel(string panel)
        {
            PanelJoinTeam.Visible = false;
            PanelLobby.Visible = false;
            PanelCharacterSelect.Visible = false;
            PanelScene.Visible = false;
            PanelDecision.Visible = false;

            switch (panel)
            {
                case "Join":
                    PanelJoinTeam.Visible = true;
                    break;

                case "Lobby":
                    PanelLobby.Visible = true;
                    break;

                case "Character":
                    PanelCharacterSelect.Visible = true;
                    break;

                case "Scene":
                    PanelScene.Visible = true;
                    break;

                case "Decision":
                    PanelDecision.Visible = true;
                    break;
            }
            ViewState["CurrentPanel"] = panel;
        }
        // show team decision results and effects after all players have decided
        private void ShowTeamDecision(DecisionResult result)
        {
            lblDecisionStatus.Text = "Team chose: " + result.OptionText;

            gvEffects.DataSource = result.Effects;
            gvEffects.DataBind();

            gvEffects.Visible = true;
        }



        // Utility buttons - Refresh to resync, Logout to clear session and go to login, Back to go back to lobby (if not host)
        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            Response.Redirect(Request.RawUrl);
        }
        protected void btnLogout_Click(object sender, EventArgs e)
        {
            // 🔥 Clear ALL session data
            Session.Clear();
            Session.Abandon();

            // 🔥 Force full reload
            Response.Redirect("Authentication.aspx");
        }
        protected void btnBack_Click(object sender, EventArgs e)
        {
            string panel = ViewState["CurrentPanel"]?.ToString();
            ShowPanel("Join");
        }
        protected void btnContinue_Click(object sender, EventArgs e)
        {
            int sessionID = Convert.ToInt32(Session["SessionID"]);
            LoadDecision(sessionID);
        }
        //  move from video to decision panel



        // Utility to get current player's character for styling dialogue bubbles
        private string GetMyCharacter()
        {
            if (Session["SessionID"] == null || Session["UserID"] == null)
                return "";

            int sessionID = Convert.ToInt32(Session["SessionID"]);
            int userID = Convert.ToInt32(Session["UserID"]);

            return pRepo.GetPlayerCharacter(sessionID, userID);
        }
        // Utility to determine CSS class for dialogue bubble based on speaker (current player vs others)
        protected string GetBubbleClass(string speaker)
        {
            // current player's character
            string myCharacter = GetMyCharacter();

            if (speaker == myCharacter)
                return "chat-right"; // 🔥 YOU
            else
                return "chat-left"; // others
        }
        // This method listens for postback events triggered by JavaScript (like video end) and acts accordingly
        protected override void RaisePostBackEvent(IPostBackEventHandler sourceControl, string eventArgument)
        {
            base.RaisePostBackEvent(sourceControl, eventArgument);

            string target = Request["__EVENTTARGET"];

            if (target == "VideoEnded")
            {
                ShowPanel("Decision");
                // get session ID from session
                int sessionID = Convert.ToInt32(Session["SessionID"]);
                LoadDecision(sessionID);
            }
        }

    }
}