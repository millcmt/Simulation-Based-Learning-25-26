using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Simulation_Based_Learning.Repositories
{
    public class DialogueRepository : BaseRepository
    {

        // Get all dialogue entries for a given scene, ordered by their display order
        public DataTable GetDialogueByScene(int sceneID)
        {
            SqlCommand cmd = new SqlCommand(@"
            SELECT DialogueID, Speaker, Dialogue, DisplayOrder
            FROM Dialogue
            WHERE SceneID = @SceneID
            ORDER BY DisplayOrder");

            cmd.Parameters.AddWithValue("@SceneID", sceneID);

            return ExecuteQuery(cmd);
        }

        // Inserts a new dialogue entry for the specified scene, automatically assigning it the next display order
        public void CreateDialogue(int sceneID, string speaker, string dialogue)
        {
            SqlCommand cmd = new SqlCommand(@"
            INSERT INTO Dialogue (SceneID, Speaker, Dialogue, DisplayOrder)
            SELECT 
                @SceneID, 
                @Speaker, 
                @Dialogue, 
                ISNULL(MAX(DisplayOrder), 0) + 1
            FROM Dialogue
            WHERE SceneID = @SceneID");

            cmd.Parameters.AddWithValue("@SceneID", sceneID);
            cmd.Parameters.AddWithValue("@Speaker", speaker);
            cmd.Parameters.AddWithValue("@Dialogue", dialogue);

            ExecuteNonQuery(cmd);
        }

        // Deletes the specified dialogue entry and shifts up the display order of any subsequent entries in the same scene
        public void DeleteDialogue(int dialogueID)
        {
            SqlCommand cmd = new SqlCommand(@"
            DELETE FROM Dialogue 
            WHERE DialogueID = @ID");

            cmd.Parameters.AddWithValue("@ID", dialogueID);

            ExecuteNonQuery(cmd);
        }

        // Updates the speaker and dialogue text of the specified dialogue entry
        public void UpdateDialogue(int dialogueID, string speaker, string dialogue)
        {
            SqlCommand cmd = new SqlCommand(@"
        UPDATE Dialogue
        SET Speaker = @Speaker,
            Dialogue = @Dialogue
        WHERE DialogueID = @ID");

            cmd.Parameters.AddWithValue("@Speaker", speaker);
            cmd.Parameters.AddWithValue("@Dialogue", dialogue);
            cmd.Parameters.AddWithValue("@ID", dialogueID);

            ExecuteNonQuery(cmd);
        }

        // swaps the display order of the specified dialogue with the one to its left or right
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