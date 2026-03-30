using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Simulation_Based_Learning.Repositories
{
    public class UserRepository : BaseRepository
    {
        // Method to create a new user - used for registration
        public void CreateUser(string username, string email, string password, string role)
        {
            string query = @"INSERT INTO [User] (Username, Email, PasswordHash, Role)
                         VALUES (@Username, @Email, @Password, @Role)";
            SqlCommand cmd = new SqlCommand(query);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Password", password);
            cmd.Parameters.AddWithValue("@Role", role);
            ExecuteNonQuery(cmd);
        }

        //method to get user by email - used for login validation
        public DataRow GetUserByEmail(string email)
        {
            // SQL query to select user details based on email
            string query = "SELECT UserID, PasswordHash, Role FROM [User] WHERE Email = @Email";
            // Create a SqlCommand object and add the email parameter to prevent SQL injection
            SqlCommand cmd = new SqlCommand(query);
            cmd.Parameters.AddWithValue("@Email", email);
            // Execute the query and get the results in a DataTable 
            DataTable dt = ExecuteQuery(cmd);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        // Method to create a guest user - used for guest access without registration
        public int CreateGuestUser()
        {
            string query = @"INSERT INTO [User] (Username, Role, IsGuest)
                           OUTPUT INSERTED.UserID
                           VALUES ('Guest', 'Guest', 1)
                            SET @NewID = SCOPE_IDENTITY()

                            UPDATE [User]
                            SET Username = 'Guest' + CAST(@NewID AS NVARCHAR)
                            WHERE UserID = @NewID

                            SELECT @NewID";
            SqlCommand cmd = new SqlCommand(query);
            return (int)ExecuteScalar(cmd);
        }
    }
}