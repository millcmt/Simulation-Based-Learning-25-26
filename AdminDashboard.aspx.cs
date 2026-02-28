using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using Simulation_Based_Learning.Repositories;

namespace Simulation_Based_Learning
{
    public partial class AdminDashboard : Page
    {
        // Repository instance for data access
        private SimulationRepository repo = new SimulationRepository();
        private PhaseRepository phaseRepo = new PhaseRepository();
        private SceneRepository sceneRepo = new SceneRepository();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                pnlSimulations.Visible = true;
                LoadSimulations();
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

        private void LoadScenesForPhase(int phaseTemplateID)
        {
            gvScenes.DataSource =
                sceneRepo.GetScenesByPhaseTemplate(phaseTemplateID);

            gvScenes.DataBind();
        }
        private void LoadScenes(int simulationID)
        {
            gvScenes.DataSource = new SceneRepository().GetScenesBySimulation(simulationID);
            gvScenes.DataBind();
        }

        private void LoadTemplates()
        {
            ddlPhaseTemplates.DataSource = phaseRepo.GetAllPhaseTemplates();
            ddlPhaseTemplates.DataTextField = "PhaseTitle";
            ddlPhaseTemplates.DataValueField = "PhaseTemplateID";
            ddlPhaseTemplates.DataBind();
        }
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

                pnlSimulations.Visible = false;
                pnlPhases.Visible = true;

                LoadPhases(simID);
                LoadTemplates();
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


                ViewState["SelectedPhaseTemplateID"] = phaseTemplateID;

                pnlScenes.Visible = true;

                LoadScenesForPhase(phaseTemplateID);
            }



        }

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
            if (ViewState["SelectedSimulationID"] != null)
            {
                int simID = Convert.ToInt32(ViewState["SelectedSimulationID"]);

                int templateID = phaseRepo.CreatePhaseTemplate(
                    txtPhaseTitle.Text,
                    txtObjective.Text
                );

                phaseRepo.AddPhaseToSimulation(simID, templateID);

                txtPhaseTitle.Text = "";
                txtObjective.Text = "";

                LoadTemplates();
                LoadPhases(simID);
            }
        }

        protected void btnAddScene_Click(object sender, EventArgs e)
        {
            if (ViewState["SelectedPhaseTemplateID"] == null)
                return;

            int phaseTemplateID = (int)ViewState["SelectedPhaseTemplateID"];

            string title = txtSceneTitle.Text.Trim();

            if (string.IsNullOrEmpty(title))
                return;

            int nextOrder = sceneRepo.GetNextSceneOrder(phaseTemplateID);

            sceneRepo.CreateScene(phaseTemplateID, title, nextOrder);

            txtSceneTitle.Text = "";

            LoadScenesForPhase(phaseTemplateID);
        }

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

        // Event handlers for navigation buttons
        protected void ShowSimulations(object sender, EventArgs e)
        {
            pnlSimulations.Visible = true;
            pnlPhases.Visible = false;
            pnlReports.Visible = false;
            pnlAudit.Visible = false;

            ViewState["SelectedSimulationID"] = null;
        }
        protected void ShowReports(object sender, EventArgs e)
        {
            pnlSimulations.Visible = false;
            pnlPhases.Visible = false;
            pnlReports.Visible = true;
            pnlAudit.Visible = false;
        }
        protected void ShowAudit(object sender, EventArgs e)
        {
            pnlSimulations.Visible = false;
            pnlPhases.Visible = false;
            pnlReports.Visible = false;
            pnlAudit.Visible = true;
        }
    }
}