using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Simulation_Based_Learning.Repositories
{
    public class DialogueRepository
    {
        private readonly string _connStr =
            ConfigurationManager.ConnectionStrings["SimDB"].ConnectionString;

        public DataTable GetDialogueByScene(int sceneID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"SELECT DialogueID, Speaker, Dialogue, DisplayOrder
                             FROM Dialogue
                             WHERE SceneID = @SceneID
                             ORDER BY DisplayOrder";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@SceneID", sceneID);

                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public void CreateDialogue(int sceneID, string speaker, string dialogue)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                string orderQuery = @"
                SELECT ISNULL(MAX(DisplayOrder),0)+1
                FROM Dialogue
                WHERE SceneID = @SceneID";

                SqlCommand orderCmd = new SqlCommand(orderQuery, con);
                orderCmd.Parameters.AddWithValue("@SceneID", sceneID);

                int nextOrder = (int)orderCmd.ExecuteScalar();

                string insertQuery = @"
                INSERT INTO Dialogue (SceneID, Speaker, Dialogue, DisplayOrder)
                VALUES (@SceneID, @Speaker, @Dialogue, @Order)";

                SqlCommand cmd = new SqlCommand(insertQuery, con);

                cmd.Parameters.AddWithValue("@SceneID", sceneID);
                cmd.Parameters.AddWithValue("@Speaker", speaker);
                cmd.Parameters.AddWithValue("@Dialogue", dialogue);
                cmd.Parameters.AddWithValue("@Order", nextOrder);

                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteDialogue(int dialogueID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "DELETE FROM Dialogue WHERE DialogueID = @ID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ID", dialogueID);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateDialogue(int dialogueID, string speaker, string dialogue)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        UPDATE Dialogue
        SET Speaker = @Speaker,
            Dialogue = @Dialogue
        WHERE DialogueID = @ID";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Speaker", speaker);
                cmd.Parameters.AddWithValue("@Dialogue", dialogue);
                cmd.Parameters.AddWithValue("@ID", dialogueID);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void SwapDialogueOrder(int dialogueID, bool moveLeft)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                using (SqlTransaction tran = con.BeginTransaction())
                {
                    try
                    {
                        string getQuery = @"
                SELECT SceneID, DisplayOrder
                FROM Dialogue
                WHERE DialogueID = @ID";

                        SqlCommand getCmd = new SqlCommand(getQuery, con, tran);
                        getCmd.Parameters.AddWithValue("@ID", dialogueID);

                        SqlDataReader reader = getCmd.ExecuteReader();

                        if (!reader.Read())
                        {
                            reader.Close();
                            tran.Rollback();
                            return;
                        }

                        int sceneID = (int)reader["SceneID"];
                        int currentOrder = (int)reader["DisplayOrder"];

                        reader.Close();

                        int targetOrder = moveLeft ? currentOrder - 1 : currentOrder + 1;

                        if (targetOrder < 1)
                        {
                            tran.Rollback();
                            return;
                        }

                        string checkQuery = @"
                SELECT DialogueID
                FROM Dialogue
                WHERE SceneID = @SceneID
                AND DisplayOrder = @TargetOrder";

                        SqlCommand checkCmd = new SqlCommand(checkQuery, con, tran);
                        checkCmd.Parameters.AddWithValue("@SceneID", sceneID);
                        checkCmd.Parameters.AddWithValue("@TargetOrder", targetOrder);

                        object targetRow = checkCmd.ExecuteScalar();

                        if (targetRow == null)
                        {
                            tran.Rollback();
                            return;
                        }

                        int targetID = Convert.ToInt32(targetRow);

                        string tempUpdate = @"
                UPDATE Dialogue
                SET DisplayOrder = -1
                WHERE DialogueID = @ID";

                        SqlCommand tempCmd = new SqlCommand(tempUpdate, con, tran);
                        tempCmd.Parameters.AddWithValue("@ID", dialogueID);
                        tempCmd.ExecuteNonQuery();

                        string moveTarget = @"
                UPDATE Dialogue
                SET DisplayOrder = @CurrentOrder
                WHERE DialogueID = @TargetID";

                        SqlCommand moveTargetCmd = new SqlCommand(moveTarget, con, tran);
                        moveTargetCmd.Parameters.AddWithValue("@CurrentOrder", currentOrder);
                        moveTargetCmd.Parameters.AddWithValue("@TargetID", targetID);
                        moveTargetCmd.ExecuteNonQuery();

                        string moveOriginal = @"
                UPDATE Dialogue
                SET DisplayOrder = @TargetOrder
                WHERE DialogueID = @ID";

                        SqlCommand moveOriginalCmd = new SqlCommand(moveOriginal, con, tran);
                        moveOriginalCmd.Parameters.AddWithValue("@TargetOrder", targetOrder);
                        moveOriginalCmd.Parameters.AddWithValue("@ID", dialogueID);
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