using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Simulation_Based_Learning.Repositories
{
    public class BaseRepository
    {
        protected readonly string _connStr;

        public BaseRepository()
        {
            _connStr = ConfigurationManager
                        .ConnectionStrings["SimDB"]
                        .ConnectionString;
        }

        protected DataTable ExecuteQuery(SqlCommand cmd)
        {
            int retries = 3;

            while (true)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(_connStr))
                    {
                        cmd.Connection = con;

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            return dt;
                        }
                    }
                }
                catch (SqlException)
                {
                    if (--retries == 0) throw;
                    System.Threading.Thread.Sleep(500);
                }
            }
        }

        protected void ExecuteNonQuery(SqlCommand cmd)
        {
            int retries = 3;

            while (true)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(_connStr))
                    {
                        cmd.Connection = con;
                        con.Open();
                        cmd.ExecuteNonQuery();
                        return;
                    }
                }
                catch (SqlException)
                {
                    if (--retries == 0) throw;
                    System.Threading.Thread.Sleep(500);
                }
            }
        }

        protected object ExecuteScalar(SqlCommand cmd)
        {
            int retries = 3;

            while (true)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(_connStr))
                    {
                        cmd.Connection = con;
                        con.Open();
                        return cmd.ExecuteScalar();
                    }
                }
                catch (SqlException)
                {
                    if (--retries == 0) throw;
                    System.Threading.Thread.Sleep(500);
                }
            }
        }
        public void AddMember(int teamID, int userID)
        {
            SqlCommand cmd = new SqlCommand(@"
        IF NOT EXISTS (
            SELECT 1 FROM TeamMember 
            WHERE TeamID = @team AND UserID = @user
        )
        INSERT INTO TeamMember (TeamID, UserID)
        VALUES (@team, @user)");

            cmd.Parameters.AddWithValue("@team", teamID);
            cmd.Parameters.AddWithValue("@user", userID);

            ExecuteNonQuery(cmd);
        }

        public string GetJoinCode(int teamID)
        {
            SqlCommand cmd = new SqlCommand(
                "SELECT JoinCode FROM Team WHERE TeamID = @team");

            cmd.Parameters.AddWithValue("@team", teamID);

            object result = ExecuteScalar(cmd);

            return result?.ToString();
        }

        public DataTable GetPlayersByTeam(int teamID)
        {
            SqlCommand cmd = new SqlCommand(@"
        SELECT U.UserID, U.Username
        FROM TeamMember TM
        INNER JOIN [User] U ON TM.UserID = U.UserID
        WHERE TM.TeamID = @team");

            cmd.Parameters.AddWithValue("@team", teamID);

            return ExecuteQuery(cmd);
        }

        public DataRow GetTeamByJoinCode(string code)
        {
            SqlCommand cmd = new SqlCommand(
                "SELECT * FROM Team WHERE JoinCode = @code");

            cmd.Parameters.AddWithValue("@code", code);

            DataTable dt = ExecuteQuery(cmd);

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

    }
}