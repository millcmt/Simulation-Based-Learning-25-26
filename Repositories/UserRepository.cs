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
        public DataRow GetUserByEmail(string email)
        {
            string query = "SELECT * FROM [User] WHERE Email = @Email";

            SqlCommand cmd = new SqlCommand(query);
            cmd.Parameters.AddWithValue("@Email", email);

            DataTable dt = ExecuteQuery(cmd);

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public int CreateGuestUser()
        {
            string query = @"INSERT INTO [User] (Username, Role, IsGuest)
                         OUTPUT INSERTED.UserID
                         VALUES ('Guest', 'Guest', 1)";

            SqlCommand cmd = new SqlCommand(query);

            return (int)ExecuteScalar(cmd);
        }

        public void UpdateGuestUsername(int userID)
        {
            string query = "UPDATE [User] SET Username = @Username WHERE UserID = @UserID";

            SqlCommand cmd = new SqlCommand(query);
            cmd.Parameters.AddWithValue("@Username", "Guest" + userID);
            cmd.Parameters.AddWithValue("@UserID", userID);

            ExecuteNonQuery(cmd);
        }

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
    }



}