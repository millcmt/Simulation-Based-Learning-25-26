using Simulation_Based_Learning.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Simulation_Based_Learning
{
    public partial class AdminDashboard : Page
    {
        //************************************************************
        // Repository instance for data access
        //************************************************************
        private SimulationRepository repo = new SimulationRepository();
        private PhaseRepository phaseRepo = new PhaseRepository();
        private SceneRepository sceneRepo = new SceneRepository();
        private DialogueRepository dialogueRepo = new DialogueRepository();
        private DecisionRepository DecisionRepo = new DecisionRepository();
        private ReportRepository reportRepo = new ReportRepository();






        //************************************************************
        // Page initialization and data loading
        //************************************************************
        // Page load event handler to initialize the dashboard
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                pnlSimulations.Visible = true;
                LoadSimulations();
                LoadClusters();
                LoadBands();
            }

            if (ViewState["SelectedPhaseTitle"] != null)
            {
                lblScenePhaseTitle.Text = ViewState["SelectedPhaseTitle"].ToString();
            }
        }
        // Method to load simulations into the GridView
        private void LoadSimulations()
        {
            gvSimulations.DataSource = repo.GetAllSimulations();
            gvSimulations.DataBind();
        }
        // Method to load phases for a selected simulation
        private void LoadPhases(int simulationID)
        {
            gvPhases.DataSource = phaseRepo.GetPhasesBySimulation(simulationID);
            gvPhases.DataBind();
            LoadScenes(simulationID);
        }
        // Method to load scenes for a selected phase template
        private void LoadScenesForPhase(int phaseTemplateID)
        {
            gvScenes.DataSource =
                sceneRepo.GetScenesByPhaseTemplate(phaseTemplateID);

            gvScenes.DataBind();
        }
        // Method to load scenes for a selected simulation (used in phase management)
        private void LoadScenes(int simulationID)
        {
            gvScenes.DataSource = new SceneRepository().GetScenesBySimulation(simulationID);
            gvScenes.DataBind();
        }
        // Method to load available phase templates into the dropdown list
        private void LoadTemplates()
        {
            ddlPhaseTemplates.DataSource = phaseRepo.GetAllPhaseTemplates();
            ddlPhaseTemplates.DataTextField = "PhaseTitle";
            ddlPhaseTemplates.DataValueField = "PhaseTemplateID";
            ddlPhaseTemplates.DataBind();
        }
        // Method to load dialogue for a selected scene
        private void LoadDialogue(int sceneID)
        {
            gvDialogue.DataSource = dialogueRepo.GetDialogueByScene(sceneID);
            gvDialogue.DataBind();
        }
        // Method to load decision points for a selected scene
        void LoadDecisionPoints(int sceneID)
        {
            gvDecisionPoints.DataSource = DecisionRepo.GetDecisionPoints(sceneID);
            gvDecisionPoints.DataBind();

            ViewState["CurrentSceneID"] = sceneID;

            pnlDecisionPoints.Visible = true;
        }
        // Method to load options for a selected decision point
        void LoadOptions(int decisionID)
        {
            gvOptions.DataSource = DecisionRepo.GetOptions(decisionID);
            gvOptions.DataBind();
        }
        // Method to load attribute effects for a selected option
        void LoadEffects(int optionID)
        {
            gvEffects.DataSource = DecisionRepo.GetAttributeEffects(optionID);
            gvEffects.DataBind();
        }
        // Method to load attributes into the GridView and dropdown list
        void LoadAttributes()
        {
            gvAttributes.DataSource = DecisionRepo.GetAttributes();
            gvAttributes.DataBind();

            ddlAttributes.DataSource = DecisionRepo.GetAttributes();
            ddlAttributes.DataTextField = "AttributeName";
            ddlAttributes.DataValueField = "AttributeID";
            ddlAttributes.DataBind();
        }

        private void LoadClusters()
        {
            gvClusters.DataSource = reportRepo.GetAllClusters();
            gvClusters.DataBind();
            ddlCluster.DataSource = reportRepo.GetAllClusters();
            ddlCluster.DataTextField = "ClusterName";
            ddlCluster.DataValueField = "ClusterID";
            ddlCluster.DataBind();
        }
        private void LoadAttributes(int clusterID)
        {
            var allAttributes = reportRepo.GetAllAttributes();
            var mapped = reportRepo.GetClusterAttributes(clusterID);

            cblAttributes.Items.Clear();

            foreach (DataRow row in allAttributes.Rows)
            {
                string id = row["AttributeID"].ToString();
                string name = row["AttributeName"].ToString();

                ListItem item = new ListItem(name, id);

                // Check if already mapped
                if (mapped.AsEnumerable().Any(r => r["AttributeID"].ToString() == id))
                {
                    item.Selected = true;
                }

                cblAttributes.Items.Add(item);
            }
        }
        private void ComputeClusterBounds(int clusterID)
        {
            int T = reportRepo.GetTotalOccurrences(clusterID);
            if (T == 0) return;
            int Emax = 3;

            int max = T * Emax;
            int min = -(T * Emax);
            int range = max - min;

            double lowUpper = min + (range * 0.3333);
            double moderateUpper = min + (range * 0.6666);

            reportRepo.SaveClusterBounds(clusterID, T, Emax, min, max, range, lowUpper, moderateUpper);
        }
        private void LoadBands()
        {
            gvBands.DataSource = reportRepo.GetAllBandDefinitions();
            gvBands.DataBind();
        }










        //************************************************************
        // Event handlers for GridView commands and button clicks
        //************************************************************
        // Event handler for GridView commands (edit, delete, manage structure)
        protected void gvSimulations_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            // Handle delete command
            if (e.CommandName == "DeleteSim")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                int simID = Convert.ToInt32(gvSimulations.DataKeys[index].Value);

                repo.DeleteSimulation(simID);
                LoadSimulations();
            }
            // Handle edit command
            if (e.CommandName == "EditSim")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                int simID = Convert.ToInt32(gvSimulations.DataKeys[index].Value);

                DataRow sim = repo.GetSimulationById(simID);

                if (sim != null)
                {
                    txtTitle.Text = sim["Title"].ToString();
                    ddlStatus.SelectedValue = sim["Status"].ToString();

                    ViewState["EditingID"] = simID;
                    btnCreate.Text = "Update Simulation";
                }
            }
            // Handle manage structure command
            if (e.CommandName == "StructureSim")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                int simID = Convert.ToInt32(gvSimulations.DataKeys[index].Value);

                ViewState["SelectedSimulationID"] = simID;

                DataRow sim = repo.GetSimulationById(simID);

                if (sim != null)
                    lblSimulationName.Text = sim["Title"].ToString();

                LoadPhases(simID);
                LoadTemplates();
                SetPanel("Phases");
            }
        }
        // Event handler for deleting a phase from the GridView
        protected void gvPhases_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "DeletePhase")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                int simulationPhaseID = Convert.ToInt32(gvPhases.DataKeys[index].Value);

                phaseRepo.DeletePhase(simulationPhaseID);

                int simID = Convert.ToInt32(ViewState["SelectedSimulationID"]);
                LoadPhases(simID);
            }
            // Handle move left/right commands for reordering phases
            if (e.CommandName == "MoveLeft" || e.CommandName == "MoveRight")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                int simPhaseID = Convert.ToInt32(gvPhases.DataKeys[index].Value);

                int simID = Convert.ToInt32(ViewState["SelectedSimulationID"]);

                bool moveLeft = e.CommandName == "MoveLeft";

                phaseRepo.SwapPhaseOrder(simID, simPhaseID, moveLeft);

                LoadPhases(simID);
            }
            // Handle view scenes command to display scenes for the selected phase
            if (e.CommandName == "ViewScenes")
            {
                int index = Convert.ToInt32(e.CommandArgument);

                int simulationPhaseID = Convert.ToInt32(
                    gvPhases.DataKeys[index].Values["SimulationPhaseID"]
                );

                int phaseTemplateID = Convert.ToInt32(
                    gvPhases.DataKeys[index].Values["PhaseTemplateID"]
                );

                string phaseTitle = gvPhases.Rows[index].Cells[0].Text;

                ViewState["SelectedPhaseTemplateID"] = phaseTemplateID;
                ViewState["SelectedPhaseTitle"] = phaseTitle;

                lblScenePhaseTitle.Text = phaseTitle;

                SetPanel("Scenes");


                LoadScenesForPhase(phaseTemplateID);
            }
            if (e.CommandName == "EditPhase")
            {
                int index = Convert.ToInt32(e.CommandArgument);

                int templateID = Convert.ToInt32(
                    gvPhases.DataKeys[index].Values["PhaseTemplateID"]
                );

                DataRow phase = phaseRepo.GetPhaseTemplateById(templateID);

                if (phase != null)
                {
                    txtPhaseTitle.Text = phase["PhaseTitle"].ToString();
                    txtObjective.Text = phase["Objective"].ToString();

                    ViewState["EditingPhaseTemplateID"] = templateID;

                    btnAddPhase.Text = "Update Phase";
                }
            }


        }
        // Event handler for deleting a scene, moving it left/right, or managing dialogue from the GridView
        protected void gvScenes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int index = Convert.ToInt32(e.CommandArgument);
            int sceneID = Convert.ToInt32(gvScenes.DataKeys[index].Value);

            string sceneTitle = gvScenes.Rows[index].Cells[0].Text;

            if (e.CommandName == "DeleteScene")
            {
                sceneRepo.DeleteScene(sceneID);

                int phaseID = (int)ViewState["SelectedPhaseTemplateID"];
                LoadScenesForPhase(phaseID);
            }

            if (e.CommandName == "MoveLeftScene" || e.CommandName == "MoveRightScene")
            {
                bool moveLeft = e.CommandName == "MoveLeftScene";

                sceneRepo.SwapSceneOrder(sceneID, moveLeft);

                int phaseID = (int)ViewState["SelectedPhaseTemplateID"];
                LoadScenesForPhase(phaseID);
            }

            if (e.CommandName == "EditScene")
            {
                DataRow scene = sceneRepo.GetSceneByID(sceneID);

                if (scene != null)
                {
                    txtSceneTitle.Text = scene["SceneTitle"].ToString();
                    txtVideoPath.Text = scene["VideoPath"].ToString();
                    txtImagePath.Text = scene["ImagePath"].ToString();
                    ViewState["EditingSceneID"] = sceneID;

                    btnAddScene.Text = "Update Scene";
                }
            }

            if (e.CommandName == "ManageDecisions")
            {
                ViewState["SelectedSceneID"] = sceneID;

                lblSceneNameDecision.Text = sceneTitle;
                lblSceneName.Text = sceneTitle;

                SetPanel("DecisionPoints");

                LoadDecisionPoints(sceneID);
            }

            if (e.CommandName == "ManageDialogue")
            {
                ViewState["SelectedSceneID"] = sceneID;


                SetPanel("Dialogue");

                LoadDialogue(sceneID);
            }
        }
        // Event handler for deleting a dialogue from the GridView
        protected void gvDialogue_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int index = Convert.ToInt32(e.CommandArgument);

            int dialogueID = Convert.ToInt32(
                gvDialogue.DataKeys[index].Value
            );

            int sceneID = Convert.ToInt32(ViewState["SelectedSceneID"]);

            if (e.CommandName == "MoveLeft")
            {
                dialogueRepo.SwapDialogueOrder(dialogueID, true);
                LoadDialogue(sceneID);
            }

            if (e.CommandName == "MoveRight")
            {
                dialogueRepo.SwapDialogueOrder(dialogueID, false);
                LoadDialogue(sceneID);
            }

            if (e.CommandName == "DeleteDialogue")
            {
                dialogueRepo.DeleteDialogue(dialogueID);
                LoadDialogue(sceneID);
            }

            if (e.CommandName == "EditDialogue")
            {
                ViewState["EditingDialogueID"] = dialogueID;

                txtSpeaker.Text = gvDialogue.Rows[index].Cells[0].Text;
                txtDialogue.Text = gvDialogue.Rows[index].Cells[1].Text;

                btnAddDialogue.Text = "Update Dialogue";
            }
        }
        // Event handler for managing decision points of a scene from the GridView
        protected void gvDecisionPoints_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int index = Convert.ToInt32(e.CommandArgument);

            int decisionID = Convert.ToInt32(
                gvDecisionPoints.DataKeys[index].Value
            );

            if (e.CommandName == "ManageOptions")
            {
                ViewState["CurrentDecisionID"] = decisionID;

                LoadOptions(decisionID);

                pnlOptions.Visible = true;
            }

            if (e.CommandName == "DeleteDecision")
            {
                DecisionRepo.DeleteDecisionPoint(decisionID);

                LoadDecisionPoints((int)ViewState["CurrentSceneID"]);
            }

            if (e.CommandName == "EditDecision")
            {
                ViewState["EditingDecisionID"] = decisionID;

                txtDecisionPrompt.Text =
                    gvDecisionPoints.Rows[index].Cells[0].Text;
            }
        }
        // Event handler for deleting an option from the GridView
        protected void gvOptions_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int index = Convert.ToInt32(e.CommandArgument);

            int optionID = Convert.ToInt32(
                gvOptions.DataKeys[index].Value
            );

            if (e.CommandName == "ManageEffects")
            {
                ViewState["CurrentOptionID"] = optionID;

                LoadEffects(optionID);
                LoadAttributes();

                pnlAttributeEffects.Visible = true;
            }

            if (e.CommandName == "DeleteOption")
            {
                DecisionRepo.DeleteOption(optionID);

                LoadOptions((int)ViewState["CurrentDecisionID"]);
            }

            if (e.CommandName == "EditOption")
            {
                ViewState["EditingOptionID"] = optionID;

                txtOptionLabel.Text = gvOptions.Rows[index].Cells[0].Text;
                txtOptionText.Text = gvOptions.Rows[index].Cells[1].Text;
            }
        }
        // Event handler for deleting an attribute or editing it from the GridView
        protected void gvAttributes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int index = Convert.ToInt32(e.CommandArgument);

            int attributeID = Convert.ToInt32(
                gvAttributes.DataKeys[index].Value
            );

            if (e.CommandName == "DeleteAttribute")
            {
                DecisionRepo.DeleteAttribute(attributeID);

                LoadAttributes();
            }

            if (e.CommandName == "EditAttribute")
            {
                ViewState["EditingAttributeID"] = attributeID;

                txtAttributeName.Text =
                    gvAttributes.Rows[index].Cells[0].Text;
            }
        }
        // Event handler for deleting an attribute effect from the GridView
        protected void gvEffects_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int index = Convert.ToInt32(e.CommandArgument);

            int effectID = Convert.ToInt32(
                gvEffects.DataKeys[index].Value
            );

            if (e.CommandName == "DeleteEffect")
            {
                DecisionRepo.DeleteEffect(effectID);

                LoadEffects((int)ViewState["CurrentOptionID"]);
            }
        }
        protected void gvClusters_RowEditing(object sender, GridViewEditEventArgs e)
        {
            gvClusters.EditIndex = e.NewEditIndex;
            LoadClusters();
        }

        protected void gvClusters_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gvClusters.EditIndex = -1;
            LoadClusters();
        }

        protected void gvClusters_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            int clusterID = Convert.ToInt32(gvClusters.DataKeys[e.RowIndex].Value);

            TextBox txtName = (TextBox)gvClusters.Rows[e.RowIndex].FindControl("txtEditName");
            string newName = txtName.Text.Trim();

            reportRepo.UpdateCluster(clusterID, newName);

            gvClusters.EditIndex = -1;
            LoadClusters();
        }
        protected void gvClusters_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int clusterID = Convert.ToInt32(gvClusters.DataKeys[e.RowIndex].Value);

            reportRepo.DeleteCluster(clusterID);

            LoadClusters();
        }
        protected void gvClusters_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "MapAttributes")
            {
                int clusterID = Convert.ToInt32(e.CommandArgument);

                hfSelectedClusterID.Value = clusterID.ToString();

                LoadAttributes(clusterID);

                pnlMapping.Visible = true;
            }
        }
        protected void gvBands_RowEditing(object sender, GridViewEditEventArgs e)
        {
            gvBands.EditIndex = e.NewEditIndex;
            LoadBands();
        }

        protected void gvBands_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gvBands.EditIndex = -1;
            LoadBands();
        }

        protected void gvBands_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            int id = Convert.ToInt32(gvBands.DataKeys[e.RowIndex].Value);

            TextBox txt = (TextBox)gvBands.Rows[e.RowIndex].FindControl("txtEditNarrative");
            string narrative = txt.Text.Trim();

            reportRepo.UpdateBand(id, narrative);

            gvBands.EditIndex = -1;
            LoadBands();
        }
        protected void gvBands_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(gvBands.DataKeys[e.RowIndex].Value);

            reportRepo.DeleteBand(id);

            LoadBands();
        }








        //************************************************************
        // Event handlers for creating/updating simulations, adding phases and scenes
        //************************************************************
        // Event handler for creating a new simulation
        protected void btnCreate_Click(object sender, EventArgs e)
        {
            if (ViewState["EditingID"] != null)
            {
                int id = Convert.ToInt32(ViewState["EditingID"]);

                repo.UpdateSimulation(id, txtTitle.Text, ddlStatus.SelectedValue);

                ViewState["EditingID"] = null;
                btnCreate.Text = "Create Simulation";
            }
            else
            {
                repo.CreateSimulation(txtTitle.Text, ddlStatus.SelectedValue);
            }

            txtTitle.Text = "";
            LoadSimulations();
        }
        // Event handler for adding a new phase to the selected simulation
        protected void btnCreateTemplate_Click(object sender, EventArgs e)
        {
            if (ViewState["EditingPhaseTemplateID"] != null)
            {
                int templateID =
                    Convert.ToInt32(ViewState["EditingPhaseTemplateID"]);

                phaseRepo.UpdatePhaseTemplate(
                    templateID,
                    txtPhaseTitle.Text,
                    txtObjective.Text
                );

                ViewState["EditingPhaseTemplateID"] = null;

                btnAddPhase.Text = "Add Phase";
            }
            else
            {
                int simID = Convert.ToInt32(ViewState["SelectedSimulationID"]);

                int templateID = phaseRepo.CreatePhaseTemplate(
                    txtPhaseTitle.Text,
                    txtObjective.Text
                );

                phaseRepo.AddPhaseToSimulation(simID, templateID);
            }

            txtPhaseTitle.Text = "";
            txtObjective.Text = "";

            LoadTemplates();
            LoadPhases(Convert.ToInt32(ViewState["SelectedSimulationID"]));
        }
        // Event handler for adding a new scene to the selected phase template
        protected void btnAddScene_Click(object sender, EventArgs e)
        {
            if (ViewState["SelectedPhaseTemplateID"] == null)
                return;

            int phaseTemplateID = (int)ViewState["SelectedPhaseTemplateID"];

            string title = txtSceneTitle.Text.Trim();
            string videoPath = txtVideoPath.Text.Trim();
            string ImagePath = txtImagePath.Text.Trim();

            if (string.IsNullOrEmpty(title))
                return;

            if (ViewState["EditingSceneID"] != null)
            {
                int sceneID = (int)ViewState["EditingSceneID"];

                sceneRepo.UpdateScene(sceneID, title, videoPath, ImagePath);

                ViewState["EditingSceneID"] = null;

                btnAddScene.Text = "Add Scene";
            }
            else
            {
                int nextOrder = sceneRepo.GetNextSceneOrder(phaseTemplateID);

                sceneRepo.CreateScene(phaseTemplateID, title, videoPath, ImagePath, nextOrder);
            }

            txtSceneTitle.Text = "";
            txtVideoPath.Text = "";
            txtImagePath.Text = "";

            LoadScenesForPhase(phaseTemplateID);
        }
        // Event handler for adding an existing phase template to the selected simulation
        protected void btnAddExistingPhase_Click(object sender, EventArgs e)
        {
            if (ViewState["SelectedSimulationID"] != null &&
                ddlPhaseTemplates.SelectedValue != "")
            {
                int simID = Convert.ToInt32(ViewState["SelectedSimulationID"]);
                int templateID = Convert.ToInt32(ddlPhaseTemplates.SelectedValue);

                phaseRepo.AddPhaseToSimulation(simID, templateID);

                LoadPhases(simID);
            }
        }
        // Event handler for adding dialogue to the selected scene
        protected void btnAddDialogue_Click(object sender, EventArgs e)
        {
            int sceneID = Convert.ToInt32(ViewState["SelectedSceneID"]);

            string speaker = txtSpeaker.Text;
            string dialogue = txtDialogue.Text;

            if (ViewState["EditingDialogueID"] != null)
            {
                int dialogueID = Convert.ToInt32(ViewState["EditingDialogueID"]);

                dialogueRepo.UpdateDialogue(dialogueID, speaker, dialogue);

                ViewState["EditingDialogueID"] = null;
                btnAddDialogue.Text = "Add Dialogue";
            }
            else
            {
                dialogueRepo.CreateDialogue(sceneID, speaker, dialogue);
            }

            txtSpeaker.Text = "";
            txtDialogue.Text = "";

            LoadDialogue(sceneID);
        }
        // Event handler for adding a decision point to the selected scene
        protected void btnAddDecisionPoint_Click(object sender, EventArgs e)
        {
            int sceneID = (int)ViewState["CurrentSceneID"];

            string prompt = txtDecisionPrompt.Text;

            if (ViewState["EditingDecisionID"] != null)
            {
                int decisionID = Convert.ToInt32(ViewState["EditingDecisionID"]);

                DecisionRepo.UpdateDecisionPoint(decisionID, prompt);

                ViewState["EditingDecisionID"] = null;

                btnAddDecisionPoint.Text = "Add Decision Point";
            }
            else
            {
                DecisionRepo.AddDecisionPoint(sceneID, prompt);
            }

            txtDecisionPrompt.Text = "";

            LoadDecisionPoints(sceneID);
        }
        // Event handler for adding an attribute to the system
        protected void btnAddAttribute_Click(object sender, EventArgs e)
        {
            string name = txtAttributeName.Text;

            if (ViewState["EditingAttributeID"] != null)
            {
                int attributeID = Convert.ToInt32(ViewState["EditingAttributeID"]);

                DecisionRepo.UpdateAttribute(attributeID, name);

                ViewState["EditingAttributeID"] = null;

                btnAddAttribute.Text = "Add Attribute";
            }
            else
            {
                DecisionRepo.AddAttribute(name);
            }

            txtAttributeName.Text = "";

            LoadAttributes();
        }
        // Event handler for adding an option to the selected decision point
        protected void btnAddOption_Click(object sender, EventArgs e)
        {
            int decisionID = (int)ViewState["CurrentDecisionID"];

            string label = txtOptionLabel.Text;
            string text = txtOptionText.Text;

            if (ViewState["EditingOptionID"] != null)
            {
                int optionID = Convert.ToInt32(ViewState["EditingOptionID"]);

                DecisionRepo.UpdateOption(optionID, label, text);

                ViewState["EditingOptionID"] = null;

                btnAddOption.Text = "Add Option";
            }
            else
            {
                DecisionRepo.AddOption(decisionID, label, text);
            }

            txtOptionLabel.Text = "";
            txtOptionText.Text = "";

            LoadOptions(decisionID);
        }
        // Event handler for adding an attribute effect to the selected option  
        protected void btnAddEffect_Click(object sender, EventArgs e)
        {
            int optionID = (int)ViewState["CurrentOptionID"];

            int attributeID = Convert.ToInt32(ddlAttributes.SelectedValue);

            int value = Convert.ToInt32(ddlEffectValue.SelectedValue);

            DecisionRepo.AddAttributeEffect(optionID, attributeID, value);

            LoadEffects(optionID);
        }

        protected void btnAddCluster_Click(object sender, EventArgs e)
        {
            string name = txtClusterName.Text.Trim();

            if (!string.IsNullOrEmpty(name))
            {
                reportRepo.InsertCluster(name);
                txtClusterName.Text = "";
                LoadClusters();
            }
        }

        protected void btnSaveMapping_Click(object sender, EventArgs e)
        {
            int clusterID = Convert.ToInt32(hfSelectedClusterID.Value);

            List<int> selectedAttributes = new List<int>();

            foreach (ListItem item in cblAttributes.Items)
            {
                if (item.Selected)
                {
                    selectedAttributes.Add(Convert.ToInt32(item.Value));
                }
            }

            // Save mapping
            reportRepo.SaveClusterAttributes(clusterID, selectedAttributes);

            // 🔥 Auto compute bounds
            ComputeClusterBounds(clusterID);

            pnlMapping.Visible = false;

            // Optional reload
            LoadClusters();
        }
        protected void btnAddBand_Click(object sender, EventArgs e)
        {
            int clusterID = Convert.ToInt32(ddlCluster.SelectedValue);
            string band = ddlBand.SelectedValue;
            string narrative = txtNarrative.Text.Trim();

            if (!string.IsNullOrEmpty(narrative))
            {
                reportRepo.InsertBand(clusterID, band, narrative);
                txtNarrative.Text = "";
                LoadBands();
            }
        }









        //************************************************************
        // Event handlers for navigation buttons
        //************************************************************
        private void SetPanel(string panel)
        {
            pnlSimulations.Visible = false;
            pnlPhases.Visible = false;
            pnlScenes.Visible = false;
            pnlDialogue.Visible = false;
            pnlDecisionPoints.Visible = false;
            pnlOptions.Visible = false;
            pnlAttributes.Visible = false;
            pnlAttributeEffects.Visible = false;
            pnlReports.Visible = false;
            pnlAudit.Visible = false;
            pnlClusters.Visible = false;
            pnlBands.Visible = false;

            btnBack.Visible = panel != "Simulations";

            switch (panel)
            {
                case "Simulations":
                    pnlSimulations.Visible = true;
                    break;

                case "Phases":
                    pnlPhases.Visible = true;
                    break;

                case "Scenes":
                    pnlScenes.Visible = true;
                    break;

                case "Dialogue":
                    pnlDialogue.Visible = true;
                    break;

                case "DecisionPoints":
                    pnlDecisionPoints.Visible = true;
                    break;

                case "Options":
                    pnlOptions.Visible = true;
                    break;

                case "Attributes":
                    pnlAttributes.Visible = true;
                    break;

                case "AttributeEffects":
                    pnlAttributeEffects.Visible = true;
                    break;

                case "Clusters":
                    pnlClusters.Visible = true;
                    break;

                case "Bands":
                    pnlBands.Visible = true;
                    break;

                case "Reports":
                    pnlReports.Visible = true;
                    break;

                case "Audit":
                    pnlAudit.Visible = true;
                    break;
            }

            ViewState["CurrentPanel"] = panel;
        }
        // Event handler for the back button to navigate to the previous panel
        protected void btnBack_Click(object sender, EventArgs e)
        {
            string panel = ViewState["CurrentPanel"]?.ToString();

            switch (panel)
            {
                case "Phases":
                    SetPanel("Simulations");
                    break;

                case "Scenes":
                    SetPanel("Phases");
                    break;

                case "Dialogue":
                    SetPanel("Scenes");
                    break;

                case "DecisionPoints":
                    SetPanel("Scenes");
                    break;

                case "Options":
                    SetPanel("DecisionPoints");
                    break;

                case "AttributeEffects":
                    SetPanel("Options");
                    break;

                case "Attributes":
                    SetPanel("Simulations");
                    break;

                case "Clusters":
                    SetPanel("Simulations");
                    break;

                case "Bands":
                    SetPanel("Simulations");
                    break;

                default:
                    SetPanel("Simulations");
                    break;
            }
        }
        protected void btnLogout_Click(object sender, EventArgs e)
        {
            // 🔥 Clear ALL session data
            Session.Clear();
            Session.Abandon();

            // 🔥 Force full reload
            Response.Redirect("Authentication.aspx");
        }

        //header buttons to switch between different panels of the dashboard
        protected void ShowSimulations(object sender, EventArgs e) { SetPanel("Simulations"); }
        protected void ShowReports(object sender, EventArgs e) { SetPanel("Reports"); }
        protected void ShowAudit(object sender, EventArgs e) { SetPanel("Audit"); }

        // footer buttons to switch between different panels of the dashboard
        protected void ShowBands(object sender, EventArgs e) { SetPanel("Bands"); }
        protected void ShowClusters(object sender, EventArgs e) { SetPanel("Clusters"); }
        protected void ShowAttributes(object sender, EventArgs e) { LoadAttributes(); SetPanel("Attributes"); }

    }
}