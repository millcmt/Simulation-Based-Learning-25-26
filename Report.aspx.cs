using Simulation_Based_Learning.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Simulation_Based_Learning
{
    public partial class Report : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                int sessionID = Convert.ToInt32(Session["SessionID"]);
                int teamID = Convert.ToInt32(Session["TeamID"]);

                ComputeClusterScore(sessionID, teamID);
                LoadReport(sessionID);
            }
        }
        ReportRepository rRepo = new ReportRepository();

        private void ComputeClusterScore(int sessionID, int teamID)
        {
            int reportID;

            // 🔒 STEP 1: Prevent duplicate reports
            if (rRepo.ReportExists(sessionID))
            {
                reportID = rRepo.GetReportID(sessionID);
                return; // IMPORTANT → stop here (no re-randomizing)
            }
            else
            {
                reportID = rRepo.CreateReport(sessionID, teamID);
            }

            // 🔥 STEP 2: Generate ONCE
            var rawScores = rRepo.GetClusterRawScores(sessionID);

            foreach (DataRow row in rawScores.Rows)
            {
                int clusterID = Convert.ToInt32(row["ClusterID"]);
                int rawScore = Convert.ToInt32(row["RawScore"]);

                DataRow bounds = rRepo.GetClusterBounds(clusterID);
                if (bounds == null) continue;

                int min = Convert.ToInt32(bounds["MinScore"]);
                int max = Convert.ToInt32(bounds["MaxScore"]);
                double lowUpper = Convert.ToDouble(bounds["LowUpper"]);
                double moderateUpper = Convert.ToDouble(bounds["ModerateUpper"]);

                double normalized = (double)(rawScore - min) / (max - min);

                string band;
                if (rawScore <= lowUpper)
                    band = "Low";
                else if (rawScore <= moderateUpper)
                    band = "Moderate";
                else
                    band = "High";

                // 🔀 RANDOMIZE ONCE HERE
                DataRow def = rRepo.GetRandomBandDefinition(clusterID, band);

                int? defID = null;
                if (def != null)
                    defID = Convert.ToInt32(def["ClusterBandDefinitionID"]);

                // 💾 SAVE LOCKED RESULT
                rRepo.InsertReportCluster(reportID, clusterID, rawScore, normalized, band, defID);
            }
        }

        private void LoadReport(int sessionID)
        {
            DataTable dt = rRepo.GetReportClusters(sessionID);

            if (dt.Rows.Count == 0)
            {
                // Optional: handle empty state
                // lblMessage.Text = "No report data available.";
                rptClusters.DataSource = null;
                rptClusters.DataBind();
                return;
            }

            rptClusters.DataSource = dt;
            rptClusters.DataBind();
        }

    }
}