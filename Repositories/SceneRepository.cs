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
                s.DisplayOrder
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
                    SELECT SceneID,
                       SceneTitle,
                       DisplayOrder
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
        public void DeleteScene(int sceneID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand(
                    "DELETE FROM Scene WHERE SceneID = @ID", con);

                cmd.Parameters.AddWithValue("@ID", sceneID);

                cmd.ExecuteNonQuery();
            }
        }

        public void SwapSceneOrder(int sceneID, bool moveLeft)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                using (SqlTransaction tran = con.BeginTransaction())
                {
                    try
                    {
                        // 1️⃣ Get current scene order and phase
                        string getQuery = @"
                    SELECT PhaseTemplateID, DisplayOrder
                    FROM Scene
                    WHERE SceneID = @SceneID";

                        SqlCommand getCmd = new SqlCommand(getQuery, con, tran);
                        getCmd.Parameters.AddWithValue("@SceneID", sceneID);

                        SqlDataReader reader = getCmd.ExecuteReader();

                        if (!reader.Read())
                        {
                            reader.Close();
                            tran.Rollback();
                            return;
                        }

                        int phaseID = Convert.ToInt32(reader["PhaseTemplateID"]);
                        int currentOrder = Convert.ToInt32(reader["DisplayOrder"]);

                        reader.Close();

                        int targetOrder = moveLeft ? currentOrder - 1 : currentOrder + 1;

                        if (targetOrder < 1)
                        {
                            tran.Rollback();
                            return;
                        }

                        // 2️⃣ Check target scene exists
                        string checkQuery = @"
                    SELECT SceneID
                    FROM Scene
                    WHERE PhaseTemplateID = @PhaseID
                    AND DisplayOrder = @TargetOrder";

                        SqlCommand checkCmd = new SqlCommand(checkQuery, con, tran);
                        checkCmd.Parameters.AddWithValue("@PhaseID", phaseID);
                        checkCmd.Parameters.AddWithValue("@TargetOrder", targetOrder);

                        object targetRow = checkCmd.ExecuteScalar();

                        if (targetRow == null)
                        {
                            tran.Rollback();
                            return;
                        }

                        int targetID = Convert.ToInt32(targetRow);

                        // 3️⃣ Move current row to TEMP
                        string tempUpdate = @"
                    UPDATE Scene
                    SET DisplayOrder = -1
                    WHERE SceneID = @SceneID";

                        SqlCommand tempCmd = new SqlCommand(tempUpdate, con, tran);
                        tempCmd.Parameters.AddWithValue("@SceneID", sceneID);
                        tempCmd.ExecuteNonQuery();

                        // 4️⃣ Move target into current spot
                        string moveTarget = @"
                    UPDATE Scene
                    SET DisplayOrder = @CurrentOrder
                    WHERE SceneID = @TargetID";

                        SqlCommand moveTargetCmd = new SqlCommand(moveTarget, con, tran);
                        moveTargetCmd.Parameters.AddWithValue("@CurrentOrder", currentOrder);
                        moveTargetCmd.Parameters.AddWithValue("@TargetID", targetID);
                        moveTargetCmd.ExecuteNonQuery();

                        // 5️⃣ Move original into target spot
                        string moveOriginal = @"
                    UPDATE Scene
                    SET DisplayOrder = @TargetOrder
                    WHERE SceneID = @SceneID";

                        SqlCommand moveOriginalCmd = new SqlCommand(moveOriginal, con, tran);
                        moveOriginalCmd.Parameters.AddWithValue("@TargetOrder", targetOrder);
                        moveOriginalCmd.Parameters.AddWithValue("@SceneID", sceneID);
                        moveOriginalCmd.ExecuteNonQuery();

                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public DataRow GetSceneByID(int sceneID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "SELECT * FROM Scene WHERE SceneID=@ID";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@ID", sceneID);

                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                    return dt.Rows[0];

                return null;
            }
        }

        public void UpdateScene(int sceneID, string title)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                string query = @"
        UPDATE Scene
        SET SceneTitle=@Title
        WHERE SceneID=@ID";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Title", title);
                cmd.Parameters.AddWithValue("@ID", sceneID);

                cmd.ExecuteNonQuery();
            }
        }



    }
}