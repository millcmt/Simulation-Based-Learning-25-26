using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Simulation_Based_Learning.Repositories
{
    public class SceneRepository : BaseRepository
    {
        // Get all scenes for a given simulation, ordered by phase and then scene order
        public DataTable GetScenesBySimulation(int simulationID)
        {
            SqlCommand cmd = new SqlCommand(@"
            SELECT 
                sp.DisplayOrder AS PhaseOrder,
                pt.PhaseTitle,
                s.SceneID,
                s.SceneTitle,
                s.VideoPath,
                s.ImagePath,
                s.DisplayOrder
            FROM SimulationPhase sp
            INNER JOIN PhaseTemplate pt 
                ON sp.PhaseTemplateID = pt.PhaseTemplateID
            INNER JOIN Scene s 
                ON pt.PhaseTemplateID = s.PhaseTemplateID
            WHERE sp.SimulationID = @SimID
            ORDER BY sp.DisplayOrder, s.DisplayOrder");

            cmd.Parameters.AddWithValue("@SimID", simulationID);

            return ExecuteQuery(cmd);
        }

        // Create a new scene for a given phase template
        public void CreateScene(int phaseTemplateID, string title, string videoPath, string imagePath, int order)
        {
            SqlCommand cmd = new SqlCommand(@"
            INSERT INTO Scene
            (PhaseTemplateID, SceneTitle, VideoPath, ImagePath, DisplayOrder)
            VALUES (@PhaseTemplateID, @Title, @VideoPath, @ImagePath, @Order)");

            cmd.Parameters.AddWithValue("@PhaseTemplateID", phaseTemplateID);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@VideoPath", videoPath);
            cmd.Parameters.AddWithValue("@ImagePath", imagePath);
            cmd.Parameters.AddWithValue("@Order", order);

            ExecuteNonQuery(cmd);
        }

        // Update an existing scene's details
        public void UpdateScene(int sceneID, string title, string videoPath, string imagePath)
        {
            SqlCommand cmd = new SqlCommand(@"
            UPDATE Scene
            SET SceneTitle = @Title,
                VideoPath = @VideoPath,
                ImagePath = @ImagePath
            WHERE SceneID = @ID");

            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@VideoPath", videoPath);
            cmd.Parameters.AddWithValue("@ImagePath", imagePath);
            cmd.Parameters.AddWithValue("@ID", sceneID);

            ExecuteNonQuery(cmd);
        }

        // Get all scenes for a specific phase template, ordered by display order
        public DataTable GetScenesByPhaseTemplate(int phaseTemplateID)
        {
            SqlCommand cmd = new SqlCommand(@"
            SELECT SceneID, SceneTitle, VideoPath, ImagePath, DisplayOrder
            FROM Scene
            WHERE PhaseTemplateID = @PhaseTemplateID
            ORDER BY DisplayOrder");

            cmd.Parameters.AddWithValue("@PhaseTemplateID", phaseTemplateID);

            return ExecuteQuery(cmd);
        }

        // Get the next display order for a new scene within a specific phase template
        public int GetNextSceneOrder(int phaseTemplateID)
        {
            SqlCommand cmd = new SqlCommand(@"
            SELECT ISNULL(MAX(DisplayOrder),0) + 1
            FROM Scene
            WHERE PhaseTemplateID = @PhaseTemplateID");

            cmd.Parameters.AddWithValue("@PhaseTemplateID", phaseTemplateID);

            return (int)ExecuteScalar(cmd);
        }

        // Get a single scene by its ID
        public DataRow GetSceneByID(int sceneID)
        {
            SqlCommand cmd = new SqlCommand(@"
            SELECT * FROM Scene WHERE SceneID = @ID");

            cmd.Parameters.AddWithValue("@ID", sceneID);

            DataTable dt = ExecuteQuery(cmd);

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        // Delete a scene and all related decision points and options
        public void DeleteScene(int sceneID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                using (SqlTransaction tran = con.BeginTransaction())
                {
                    try
                    {
                        SqlCommand cmd1 = new SqlCommand(@"
                    DELETE FROM [Option]
                    WHERE DecisionPointID IN (
                        SELECT DecisionPointID FROM DecisionPoint WHERE SceneID = @ID
                    )", con, tran);

                        cmd1.Parameters.AddWithValue("@ID", sceneID);
                        cmd1.ExecuteNonQuery();

                        SqlCommand cmd2 = new SqlCommand(
                            "DELETE FROM DecisionPoint WHERE SceneID = @ID", con, tran);
                        cmd2.Parameters.AddWithValue("@ID", sceneID);
                        cmd2.ExecuteNonQuery();

                        SqlCommand cmd3 = new SqlCommand(
                            "DELETE FROM Dialogue WHERE SceneID = @ID", con, tran);
                        cmd3.Parameters.AddWithValue("@ID", sceneID);
                        cmd3.ExecuteNonQuery();

                        SqlCommand cmd4 = new SqlCommand(
                            "DELETE FROM Scene WHERE SceneID = @ID", con, tran);
                        cmd4.Parameters.AddWithValue("@ID", sceneID);
                        cmd4.ExecuteNonQuery();

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

        // Swap the display order of a scene with its adjacent scene (left/up or right/down)
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

    }
}