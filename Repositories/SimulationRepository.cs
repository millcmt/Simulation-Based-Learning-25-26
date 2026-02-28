using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Simulation_Based_Learning.Repositories
{
    public class SimulationRepository
    {
        // Connection string for database access
        private readonly string _connStr;

        // Constructor to initialize the connection string from configuration
        public SimulationRepository()
        {
            _connStr = ConfigurationManager.ConnectionStrings["SimDB"].ConnectionString;
        }

        // Method to retrieve all simulations from the database
        public DataTable GetAllSimulations()
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "SELECT SimulationID, Title, Status, CreatedDate FROM Simulation";
                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // Method to delete a simulation by its ID
        public void DeleteSimulation(int simID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();
                string query = "DELETE FROM Simulation WHERE SimulationID = @SimulationID";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@SimulationID", simID);
                cmd.ExecuteNonQuery();
            }
        }

        // Method to create a new simulation with a title and status
        public void CreateSimulation(string title, string status)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                string query = @"INSERT INTO Simulation (Title, Status, CreatedDate)
                         VALUES (@Title, @Status, GETDATE())";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Method to retrieve a specific simulation by its ID
        public DataRow GetSimulationById(int simulationID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "SELECT * FROM Simulation WHERE SimulationID = @ID";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@ID", simulationID);

                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt.Rows.Count > 0 ? dt.Rows[0] : null;
            }
        }

        // Method to update an existing simulation's title and status by its ID
        public void UpdateSimulation(int id, string title, string status)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                string query = @"UPDATE Simulation 
                         SET Title = @Title,
                             Status = @Status
                         WHERE SimulationID = @ID";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.ExecuteNonQuery();
                }
            }
        }












    }
}