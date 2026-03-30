using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Simulation_Based_Learning.Repositories
{
    public class SimulationRepository : BaseRepository
    {
        // Method to retrieve all simulations from the database
        public DataTable GetAllSimulations()
        {
            SqlCommand cmd = new SqlCommand(@"
                SELECT SimulationID, Title, Status, CreatedDate 
                FROM Simulation");

            return ExecuteQuery(cmd);
        }

        // Method to delete a simulation by its ID
        public void DeleteSimulation(int simID)
        {
            SqlCommand cmd = new SqlCommand(@"
                DELETE FROM Simulation 
                WHERE SimulationID = @SimulationID");

            cmd.Parameters.AddWithValue("@SimulationID", simID);

            ExecuteNonQuery(cmd);
        }

        // Method to create a new simulation with a title and status
        public void CreateSimulation(string title, string status)
        {
            SqlCommand cmd = new SqlCommand(@"
                INSERT INTO Simulation (Title, Status, CreatedDate)
                VALUES (@Title, @Status, GETDATE())");

            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Status", status);

            ExecuteNonQuery(cmd);
        }

        // Method to retrieve a specific simulation by its ID
        public DataRow GetSimulationById(int simulationID)
        {
            SqlCommand cmd = new SqlCommand(@"
                SELECT * 
                FROM Simulation 
                WHERE SimulationID = @ID");

            cmd.Parameters.AddWithValue("@ID", simulationID);

            DataTable dt = ExecuteQuery(cmd);

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        // Method to update an existing simulation's title and status by its ID
        public void UpdateSimulation(int id, string title, string status)
        {
            SqlCommand cmd = new SqlCommand(@"
                UPDATE Simulation 
                SET Title = @Title,
                    Status = @Status
                WHERE SimulationID = @ID");

            cmd.Parameters.AddWithValue("@ID", id);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Status", status);

            ExecuteNonQuery(cmd);
        }
    }

}