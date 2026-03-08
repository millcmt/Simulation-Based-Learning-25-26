using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Simulation_Based_Learning.Repositories
{
    public class PhaseRepository
    {
        private readonly string _connStr;

        public PhaseRepository()
        {
            _connStr = ConfigurationManager
                        .ConnectionStrings["SimDB"]
                        .ConnectionString;
        }

        public DataTable GetPhasesBySimulation(int simulationID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
            SELECT sp.SimulationPhaseID,
                   sp.PhaseTemplateID,
                   pt.PhaseTitle,
                   pt.Objective,
                   sp.DisplayOrder
            FROM SimulationPhase sp
            INNER JOIN PhaseTemplate pt
                ON sp.PhaseTemplateID = pt.PhaseTemplateID
            WHERE sp.SimulationID = @SimID
            ORDER BY sp.DisplayOrder";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@SimID", simulationID);

                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

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

        public void UpdatePhaseTemplate(int id, string title, string objective)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                string query = @"
        UPDATE PhaseTemplate
        SET PhaseTitle=@Title,
            Objective=@Objective
        WHERE PhaseTemplateID=@ID";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Title", title);
                cmd.Parameters.AddWithValue("@Objective", objective);
                cmd.Parameters.AddWithValue("@ID", id);

                cmd.ExecuteNonQuery();
            }
        }
        public int CreatePhaseTemplate(string title, string objective)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                string query = @"
            INSERT INTO PhaseTemplate (PhaseTitle, Objective)
            OUTPUT INSERTED.PhaseTemplateID
            VALUES (@Title, @Objective)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Title", title);
                cmd.Parameters.AddWithValue("@Objective", objective);

                return (int)cmd.ExecuteScalar();
            }
        }

        public void AddPhaseToSimulation(int simulationID, int phaseTemplateID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                // Get next order automatically
                string orderQuery = @"
            SELECT ISNULL(MAX(DisplayOrder),0) + 1
            FROM SimulationPhase
            WHERE SimulationID = @SimID";

                SqlCommand orderCmd = new SqlCommand(orderQuery, con);
                orderCmd.Parameters.AddWithValue("@SimID", simulationID);

                int nextOrder = (int)orderCmd.ExecuteScalar();

                string insertQuery = @"
            INSERT INTO SimulationPhase
            (SimulationID, PhaseTemplateID, DisplayOrder)
            VALUES (@SimID, @TemplateID, @Order)";

                SqlCommand cmd = new SqlCommand(insertQuery, con);
                cmd.Parameters.AddWithValue("@SimID", simulationID);
                cmd.Parameters.AddWithValue("@TemplateID", phaseTemplateID);
                cmd.Parameters.AddWithValue("@Order", nextOrder);

                cmd.ExecuteNonQuery();
            }
        }

        public void DeletePhase(int simulationPhaseID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                string query = "DELETE FROM SimulationPhase WHERE SimulationPhaseID = @ID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ID", simulationPhaseID);
                cmd.ExecuteNonQuery();
            }
        }

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