using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Simulation_Based_Learning.Repositories
{
    public class PhaseRepository : BaseRepository
    {
        // Gets all phases for a given simulation, including template details, ordered by DisplayOrder
        public DataTable GetPhasesBySimulation(int simulationID)
        {
            SqlCommand cmd = new SqlCommand(@"
            SELECT sp.SimulationPhaseID,
                   sp.PhaseTemplateID,
                   pt.PhaseTitle,
                   pt.Objective,
                   sp.DisplayOrder
            FROM SimulationPhase sp
            INNER JOIN PhaseTemplate pt
                ON sp.PhaseTemplateID = pt.PhaseTemplateID
            WHERE sp.SimulationID = @SimID
            ORDER BY sp.DisplayOrder");

            cmd.Parameters.AddWithValue("@SimID", simulationID);

            return ExecuteQuery(cmd);
        }

        // Retrieves all phase templates with just ID and Title for dropdowns, ordered alphabetically
        public DataTable GetAllPhaseTemplates()
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "SELECT PhaseTemplateID, PhaseTitle FROM PhaseTemplate ORDER BY PhaseTitle";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        // Retrieves a single phase template by its ID, returns null if not found
        public DataRow GetPhaseTemplateById(int templateID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "SELECT * FROM PhaseTemplate WHERE PhaseTemplateID=@ID";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@ID", templateID);

                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                    return dt.Rows[0];

                return null;
            }
        }

        // Updates the title and objective of an existing phase template
        public void UpdatePhaseTemplate(int id, string title, string objective)
        {
            SqlCommand cmd = new SqlCommand(@"
            UPDATE PhaseTemplate
            SET PhaseTitle = @Title,
                Objective = @Objective
            WHERE PhaseTemplateID = @ID");

            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Objective", objective);
            cmd.Parameters.AddWithValue("@ID", id);

            ExecuteNonQuery(cmd);
        }

        // Creates a new phase template and returns its ID
        public int CreatePhaseTemplate(string title, string objective)
        {
            SqlCommand cmd = new SqlCommand(@"
            INSERT INTO PhaseTemplate (PhaseTitle, Objective)
            OUTPUT INSERTED.PhaseTemplateID
            VALUES (@Title, @Objective)");

            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Objective", objective);

            return (int)ExecuteScalar(cmd);
        }
        
        // Note: Deleting a template does NOT delete associated simulation phases, but they will show "Unknown Phase" in the UI
        public void DeletePhase(int simulationPhaseID)
        {
            SqlCommand cmd = new SqlCommand(@"
        DELETE FROM SimulationPhase 
        WHERE SimulationPhaseID = @ID");

            cmd.Parameters.AddWithValue("@ID", simulationPhaseID);

            ExecuteNonQuery(cmd);
        }

        // Adds a new phase to a simulation based on a template, placing it at the end of the current phases
        public void AddPhaseToSimulation(int simulationID, int phaseTemplateID)
        {
            SqlCommand cmd = new SqlCommand(@"
            INSERT INTO SimulationPhase (SimulationID, PhaseTemplateID, DisplayOrder)
            SELECT 
                @SimID, 
                @TemplateID, 
                ISNULL(MAX(DisplayOrder), 0) + 1
            FROM SimulationPhase
            WHERE SimulationID = @SimID");

            cmd.Parameters.AddWithValue("@SimID", simulationID);
            cmd.Parameters.AddWithValue("@TemplateID", phaseTemplateID);

            ExecuteNonQuery(cmd);
        }

        // Swaps the display order of a phase with its adjacent phase (left or right) within the same simulation
        public void SwapPhaseOrder(int simulationID, int phaseInstanceID, bool moveLeft)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                using (SqlTransaction tran = con.BeginTransaction())
                {
                    try
                    {
                        // 1️⃣ Get current order
                        string getQuery = @"
                    SELECT DisplayOrder
                    FROM SimulationPhase
                    WHERE SimulationPhaseID = @ID";

                        SqlCommand getCmd = new SqlCommand(getQuery, con, tran);
                        getCmd.Parameters.AddWithValue("@ID", phaseInstanceID);

                        object result = getCmd.ExecuteScalar();
                        if (result == null)
                        {
                            tran.Rollback();
                            return;
                        }

                        int currentOrder = Convert.ToInt32(result);
                        int targetOrder = moveLeft ? currentOrder - 1 : currentOrder + 1;

                        if (targetOrder < 1)
                        {
                            tran.Rollback();
                            return;
                        }

                        // 2️⃣ Check target exists
                        string checkQuery = @"
                    SELECT SimulationPhaseID
                    FROM SimulationPhase
                    WHERE SimulationID = @SimID
                    AND DisplayOrder = @TargetOrder";

                        SqlCommand checkCmd = new SqlCommand(checkQuery, con, tran);
                        checkCmd.Parameters.AddWithValue("@SimID", simulationID);
                        checkCmd.Parameters.AddWithValue("@TargetOrder", targetOrder);

                        object targetRow = checkCmd.ExecuteScalar();
                        if (targetRow == null)
                        {
                            tran.Rollback();
                            return;
                        }

                        int targetID = Convert.ToInt32(targetRow);

                        // 3️⃣ TEMP move current row to -1
                        string tempUpdate = @"
                    UPDATE SimulationPhase
                    SET DisplayOrder = -1
                    WHERE SimulationPhaseID = @ID";

                        SqlCommand tempCmd = new SqlCommand(tempUpdate, con, tran);
                        tempCmd.Parameters.AddWithValue("@ID", phaseInstanceID);
                        tempCmd.ExecuteNonQuery();

                        // 4️⃣ Move target row into current spot
                        string moveTarget = @"
                    UPDATE SimulationPhase
                    SET DisplayOrder = @CurrentOrder
                    WHERE SimulationPhaseID = @TargetID";

                        SqlCommand moveTargetCmd = new SqlCommand(moveTarget, con, tran);
                        moveTargetCmd.Parameters.AddWithValue("@CurrentOrder", currentOrder);
                        moveTargetCmd.Parameters.AddWithValue("@TargetID", targetID);
                        moveTargetCmd.ExecuteNonQuery();

                        // 5️⃣ Move original row into target spot
                        string moveOriginal = @"
                    UPDATE SimulationPhase
                    SET DisplayOrder = @TargetOrder
                    WHERE SimulationPhaseID = @ID";

                        SqlCommand moveOriginalCmd = new SqlCommand(moveOriginal, con, tran);
                        moveOriginalCmd.Parameters.AddWithValue("@TargetOrder", targetOrder);
                        moveOriginalCmd.Parameters.AddWithValue("@ID", phaseInstanceID);
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