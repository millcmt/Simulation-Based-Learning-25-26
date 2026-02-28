using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Simulation_Based_Learning.Repositories
{
    public class SceneRepository
    {
        private readonly string _connStr;

        public SceneRepository()
        {
            _connStr = ConfigurationManager
                        .ConnectionStrings["SimDB"]
                        .ConnectionString;
        }


        public DataTable GetScenesBySimulation(int simulationID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
            SELECT 
                sp.DisplayOrder AS PhaseOrder,
                pt.PhaseTitle,
                s.SceneID,
                s.SceneTitle,
                s.DisplayOrder AS SceneOrder
            FROM SimulationPhase sp
            INNER JOIN PhaseTemplate pt 
                ON sp.PhaseTemplateID = pt.PhaseTemplateID
            INNER JOIN Scene s 
                ON pt.PhaseTemplateID = s.PhaseTemplateID
            WHERE sp.SimulationID = @SimID
            ORDER BY sp.DisplayOrder, s.DisplayOrder";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@SimID", simulationID);

                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public void CreateScene(int phaseTemplateID, string title, int order)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                string query = @"
            INSERT INTO Scene
            (PhaseTemplateID, SceneTitle, DisplayOrder)
            VALUES (@PhaseTemplateID, @Title, @Order)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PhaseTemplateID", phaseTemplateID);
                cmd.Parameters.AddWithValue("@Title", title);
                cmd.Parameters.AddWithValue("@Order", order);

                cmd.ExecuteNonQuery();
            }
        }

        public DataTable GetScenesByPhaseTemplate(int phaseTemplateID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
            SELECT 
                SceneID,
                SceneTitle,
                DisplayOrder AS SceneOrder
            FROM Scene
            WHERE PhaseTemplateID = @PhaseTemplateID
            ORDER BY DisplayOrder";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@PhaseTemplateID", phaseTemplateID);

                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public int GetNextSceneOrder(int phaseTemplateID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                string query = @"
            SELECT ISNULL(MAX(DisplayOrder),0) + 1
            FROM Scene
            WHERE PhaseTemplateID = @PhaseTemplateID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PhaseTemplateID", phaseTemplateID);

                return (int)cmd.ExecuteScalar();
            }
        }






    }
}