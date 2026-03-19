using Simulation_Based_Learning.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Simulation_Based_Learning
{
    public partial class PlayThrough : System.Web.UI.Page
    {
        private SimulationRepository repo = new SimulationRepository();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["SessionID"] != null)
            {
                int sessionID = Convert.ToInt32(Session["SessionID"]);

                PlaythroughRepository repo = new PlaythroughRepository();
                string status = repo.GetSessionStatus(sessionID);

                if (status == "InProgress")
                {
                    LoadScene(sessionID);
                    return;
                }
            }
            if (!IsPostBack)
            {
                // Load available simulations
                var simulations = repo.GetAllSimulations(); // returns List<Simulation>
                ddlSimulations.DataSource = simulations;
                ddlSimulations.DataTextField = "Title";
                ddlSimulations.DataValueField = "SimulationID";
                ddlSimulations.DataBind();

                // Decide initial panel
                if (Session["TeamID"] == null)
                {
                    ShowPanel("Join");
                }
                else if (Session["SessionID"] != null)
                {
                    // Player already has a session → show Scene or Character
                    ShowPanel("Character");
                }
                else
                {
                    ShowPanel("Lobby");
                    LoadPlayers();
                }
            }
        }


        protected void btnCreateTeam_Click(object sender, EventArgs e)
        {
            string teamName = txtTeamName.Text.Trim();

            if (string.IsNullOrEmpty(teamName))
            {
                lblJoinError.Text = "Enter a team name.";
                return;
            }

            PlaythroughRepository playRepo = new PlaythroughRepository();

            int teamID = playRepo.CreateTeam(teamName);

            // host joins their own team
            playRepo.AddMember(teamID, Convert.ToInt32(Session["UserID"]));

            Session["TeamID"] = teamID;

            ShowPanel("Lobby");
            LoadPlayers();
        }
        protected void btnJoinTeam_Click(object sender, EventArgs e)
        {
            string code = txtJoinCode.Text.Trim();

            PlaythroughRepository playRepo = new PlaythroughRepository();

            DataRow team = playRepo.GetTeamByJoinCode(code);

            if (team == null)
            {
                lblJoinError.Text = "Invalid team code.";
                return;
            }

            int teamID = Convert.ToInt32(team["TeamID"]);

           

            if (playRepo.IsSessionActive(teamID))
            {
                lblJoinError.Text = "Simulation already started.";
                return;
            }

            Session["TeamID"] = teamID;

            playRepo.AddMember(teamID, Convert.ToInt32(Session["UserID"]));
            
            ShowPanel("Lobby");
            LoadPlayers();
        }


        private void LoadPlayers()
        {
            int teamID = Convert.ToInt32(Session["TeamID"]);

            PlaythroughRepository repo = new PlaythroughRepository();

            rptPlayers.DataSource = repo.GetPlayersByTeam(teamID);
            rptPlayers.DataBind();
            lblTeamCode.Text = new PlaythroughRepository().GetJoinCode(teamID);


        }
        private void LoadScene(int sessionID)
        {
            PlaythroughRepository repo = new PlaythroughRepository();

            DataRow scene = repo.GetCurrentScene(sessionID);

            if (scene == null) return;

            ShowPanel("Scene");

            lblSceneTitle.Text = scene["SceneTitle"].ToString();
            sceneSource.Src = scene["VideoPath"].ToString();
            sceneVideo.Attributes["load"] = "true";
            sceneVideo.Attributes["key"] = Guid.NewGuid().ToString();
        }

        protected void btnStartSimulation_Click(object sender, EventArgs e)
        {
            int simulationID = int.Parse(ddlSimulations.SelectedValue);
            int teamID = Convert.ToInt32(Session["TeamID"]);

            PlaythroughRepository repo = new PlaythroughRepository();

            int sessionID = repo.CreateSession(teamID, simulationID);
            repo.UpdateSessionStatus(sessionID, "Waiting"); // status before character selection
            Session["SessionID"] = sessionID;

            ShowPanel("Character"); // Move players to character selection
        }


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
        }

        protected void SelectCharacter(object sender, EventArgs e)
        {
            int teamID = Convert.ToInt32(Session["TeamID"]);
            int userID = Convert.ToInt32(Session["UserID"]);

            Button btn = (Button)sender;
            string character = btn.Text;

            PlaythroughRepository repo = new PlaythroughRepository();

            // Ensure session exists
            int sessionID;
            if (Session["SessionID"] != null)
            {
                sessionID = Convert.ToInt32(Session["SessionID"]);
            }
            else
            {
                int simulationID = int.Parse(ddlSimulations.SelectedValue);
                sessionID = repo.CreateSession(teamID, simulationID);
                repo.UpdateSessionStatus(sessionID, "InProgress");
                Session["SessionID"] = sessionID;
            }

            // Attempt to select the character
            bool success = repo.SelectCharacter(sessionID, userID, character);

            if (!success)
            {
                lblCharacterStatus.Text = $"Sorry, {character} is already taken.";
                btn.Enabled = false; // disable clicked button
                return;
            }

            lblCharacterStatus.Text = $"You have selected: {character}";

            // Disable already taken characters
            DisableTakenCharacters(sessionID);

            // Show the Character panel (post-selection)
            ShowPanel("Character");

            // Check if all players selected → auto move to Scene panel
            int totalPlayers = repo.GetPlayerCount(teamID);
            int selectedCount = repo.GetSelectedCharacterCount(sessionID);
            if (selectedCount >= totalPlayers && totalPlayers > 0)
            {
                // 🔥 Start simulation properly
                repo.UpdateSessionStatus(sessionID, "InProgress");

                // 🔥 Initialize FIRST scene
                repo.InitializeSessionProgress(sessionID);

                // 🔥 Reload page so everyone syncs
                Response.Redirect(Request.RawUrl);
            }


        }
        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            Response.Redirect(Request.RawUrl);
        }

        protected void btnContinue_Click(object sender, EventArgs e)
        {
            int sessionID = Convert.ToInt32(Session["SessionID"]);
            PlaythroughRepository pRepo = new PlaythroughRepository();
            DecisionRepository dRepo = new DecisionRepository();

            // Get the current scene
            DataRow currentScene = pRepo.GetCurrentScene(sessionID);
            if (currentScene == null) return;

            int sceneID = (int)currentScene["SceneID"];

            // Get all decision points for the current scene
            DataTable decisions = dRepo.GetDecisionPoints(sceneID);

            if (decisions.Rows.Count > 0)
            {
                // Load the first decision point
                int decisionID = (int)decisions.Rows[0]["DecisionPointID"];
                lblDecisionQuestion.Text = decisions.Rows[0]["DecisionPrompt"].ToString();

                DataTable options = dRepo.GetOptions(decisionID);
                rblOptions.DataSource = options;
                rblOptions.DataTextField = "OptionText";
                rblOptions.DataValueField = "OptionID";
                rblOptions.DataBind();

                ShowPanel("Decision");
            }
            else
            {
                // No decision points → move to next scene
                int nextSceneID = pRepo.GetNextSceneID(sessionID, sceneID); // You need to implement this
                if (nextSceneID > 0)
                {
                    pRepo.SetCurrentScene(sessionID, nextSceneID);
                    LoadScene(sessionID);
                }
                else
                {
                    // End simulation if no next scene
                    pRepo.UpdateSessionStatus(sessionID, "Completed");
                    ShowPanel("Lobby"); // or a "Simulation Complete" panel
                }
            }
        }

        protected void btnSubmitDecision_Click(object sender, EventArgs e)
        {
            if (rblOptions.SelectedItem == null)
            {
                lblDecisionStatus.Text = "Please select an option before continuing.";
                return;
            }

            int sessionID = Convert.ToInt32(Session["SessionID"]);
            int playerID = Convert.ToInt32(Session["UserID"]);
            int optionID = int.Parse(rblOptions.SelectedValue);

            PlaythroughRepository pRepo = new PlaythroughRepository();
            DecisionRepository dRepo = new DecisionRepository();

            // Get current scene
            DataRow currentScene = pRepo.GetCurrentScene(sessionID);
            if (currentScene == null) return;

            int sceneID = (int)currentScene["SceneID"];

            // Get current decision point
            DataTable decisions = dRepo.GetDecisionPoints(sceneID);
            if (decisions.Rows.Count == 0) return;

            int decisionID = (int)decisions.Rows[0]["DecisionPointID"];

            // Submit player's decision using repository method
            pRepo.SubmitPlayerDecision(sessionID, playerID, decisionID, optionID);

            // Check if all players have submitted
            if (pRepo.HaveAllPlayersDecided(sessionID, decisionID))
            {
                // Move to next scene
                int nextSceneID = pRepo.GetNextSceneID(sessionID, sceneID);
                if (nextSceneID > 0)
                {
                    pRepo.SetCurrentScene(sessionID, nextSceneID);
                    LoadScene(sessionID);
                }
                else
                {
                    // End simulation
                    pRepo.UpdateSessionStatus(sessionID, "Completed");
                    ShowPanel("Lobby"); // Or a "Simulation Complete" panel
                }
            }
            else
            {
                lblDecisionStatus.Text = "Waiting for other players to submit their decisions...";
            }
        }
        protected override void RaisePostBackEvent(IPostBackEventHandler sourceControl, string eventArgument)
        {
            base.RaisePostBackEvent(sourceControl, eventArgument);

            string target = Request["__EVENTTARGET"];

            if (target == "VideoEnded")
            {
                ShowPanel("Decision");
                LoadDecisionFromCurrentScene();
            }
        }
        private void LoadDecisionFromCurrentScene()
        {
            int sessionID = Convert.ToInt32(Session["SessionID"]);

            PlaythroughRepository pRepo = new PlaythroughRepository();
            DecisionRepository dRepo = new DecisionRepository();

            DataRow scene = pRepo.GetCurrentScene(sessionID);

            if (scene == null) return;

            int sceneID = (int)scene["SceneID"];

            DataTable decisions = dRepo.GetDecisionPoints(sceneID);

            if (decisions.Rows.Count == 0) return;

            int decisionID = (int)decisions.Rows[0]["DecisionPointID"];

            lblDecisionQuestion.Text = decisions.Rows[0]["DecisionPrompt"].ToString();

            DataTable options = dRepo.GetOptions(decisionID);

            rblOptions.DataSource = options;
            rblOptions.DataTextField = "OptionText";
            rblOptions.DataValueField = "OptionID";
            rblOptions.DataBind();
        }

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

    }
}