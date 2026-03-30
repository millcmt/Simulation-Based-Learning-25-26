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
        // Connection string for the database, initialized from the configuration file.
        protected readonly string _connStr;

        // Constructor that initializes the connection string from the configuration file.
        public BaseRepository()
        {
            _connStr = ConfigurationManager
                        .ConnectionStrings["SimDB"]
                        .ConnectionString;
        }

        // Executes a query and returns the results as a DataTable. Retries up to 3 times on failure.
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
                    System.Threading.Thread.Sleep(200);
                }
            }
        }

        // Executes a non-query command (like INSERT, UPDATE, DELETE). Retries up to 3 times on failure.
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

        // Executes a scalar command (like COUNT, SUM) and returns the result. Retries up to 3 times on failure.
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
                    System.Threading.Thread.Sleep(200);
                }
            }
        }

        // Executes a query and returns the results as a DataSet. Retries up to 3 times on failure.
        protected DataSet ExecuteDataSet(SqlCommand cmd)
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
                            DataSet ds = new DataSet();
                            da.Fill(ds);
                            return ds;
                        }
                    }
                }
                catch (SqlException)
                {
                    if (--retries == 0) throw;
                    System.Threading.Thread.Sleep(200);
                }
            }
        }
    }
}