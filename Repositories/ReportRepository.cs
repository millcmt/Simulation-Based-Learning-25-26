using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Simulation_Based_Learning.Repositories
{
    public class ReportRepository : BaseRepository
    {
        

        public DataTable GetAllClusters()
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "SELECT * FROM Cluster";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public void InsertCluster(string name)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "INSERT INTO Cluster (ClusterName) VALUES (@Name)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Name", name);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateCluster(int id, string name)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "UPDATE Cluster SET ClusterName=@Name WHERE ClusterID=@ID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@ID", id);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteCluster(int id)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "DELETE FROM Cluster WHERE ClusterID=@ID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ID", id);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public DataTable GetAllAttributes()
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "SELECT AttributeID, AttributeName FROM Attribute";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public DataTable GetClusterAttributes(int clusterID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "SELECT AttributeID FROM ClusterAttribute WHERE ClusterID=@ID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ID", clusterID);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public void SaveClusterAttributes(int clusterID, List<int> attributeIDs)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                // Remove old mappings
                SqlCommand deleteCmd = new SqlCommand(
                    "DELETE FROM ClusterAttribute WHERE ClusterID=@ID", con);
                deleteCmd.Parameters.AddWithValue("@ID", clusterID);
                deleteCmd.ExecuteNonQuery();

                // Insert new mappings
                foreach (int attrID in attributeIDs)
                {
                    SqlCommand insertCmd = new SqlCommand(
                        "INSERT INTO ClusterAttribute (ClusterID, AttributeID) VALUES (@CID, @AID)", con);

                    insertCmd.Parameters.AddWithValue("@CID", clusterID);
                    insertCmd.Parameters.AddWithValue("@AID", attrID);

                    insertCmd.ExecuteNonQuery();
                }
            }
        }
        public int GetTotalOccurrences(int clusterID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        SELECT COUNT(*) 
        FROM (
            SELECT OAE.OptionID, OAE.AttributeID
            FROM OptionAttributeEffect OAE
            INNER JOIN ClusterAttribute CA 
                ON OAE.AttributeID = CA.AttributeID
            WHERE CA.ClusterID = @ClusterID
            GROUP BY OAE.OptionID, OAE.AttributeID
        ) AS DistinctEffects";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ClusterID", clusterID);

                con.Open();
                return (int)cmd.ExecuteScalar();
            }
        }
        public void SaveClusterBounds(int clusterID, int T, int Emax, int min, int max, int range, double lowUpper, double moderateUpper)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
                IF EXISTS (SELECT 1 FROM ClusterBounds WHERE ClusterID = @ClusterID)
                BEGIN
                    UPDATE ClusterBounds
                    SET TotalOccurrences = @T,
                        MaxEffect = @Emax,
                        MinScore = @Min,
                        MaxScore = @Max,
                        RangeScore = @Range,
                        LowUpper = @LowUpper,
                        ModerateUpper = @ModerateUpper
                    WHERE ClusterID = @ClusterID
                END
                ELSE
                BEGIN
                    INSERT INTO ClusterBounds 
                    (ClusterID, TotalOccurrences, MaxEffect, MinScore, MaxScore, RangeScore, LowUpper, ModerateUpper)
                    VALUES
                    (@ClusterID, @T, @Emax, @Min, @Max, @Range, @LowUpper, @ModerateUpper)
                END
                ";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@ClusterID", clusterID);
                cmd.Parameters.AddWithValue("@T", T);
                cmd.Parameters.AddWithValue("@Emax", Emax);
                cmd.Parameters.AddWithValue("@Min", min);
                cmd.Parameters.AddWithValue("@Max", max);
                cmd.Parameters.AddWithValue("@Range", range);
                cmd.Parameters.AddWithValue("@LowUpper", lowUpper);
                cmd.Parameters.AddWithValue("@ModerateUpper", moderateUpper);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

















        public DataTable GetClusterRawScores(int sessionID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        SELECT 
            CA.ClusterID,
            SUM(OAE.EffectValue) AS RawScore
        FROM TeamDecision TD
        INNER JOIN [Option] O ON TD.SelectedOptionID = O.OptionID
        INNER JOIN OptionAttributeEffect OAE ON O.OptionID = OAE.OptionID
        INNER JOIN ClusterAttribute CA ON OAE.AttributeID = CA.AttributeID
        WHERE TD.SessionID = @SessionID
        GROUP BY CA.ClusterID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@SessionID", sessionID);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public DataRow GetClusterBounds(int clusterID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "SELECT * FROM ClusterBounds WHERE ClusterID=@ID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ID", clusterID);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt.Rows.Count > 0 ? dt.Rows[0] : null;
            }
        }

        public int CreateReport(int sessionID, int teamID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        INSERT INTO Report (SessionID, TeamID)
        OUTPUT INSERTED.ReportID
        VALUES (@SessionID, @TeamID)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@SessionID", sessionID);
                cmd.Parameters.AddWithValue("@TeamID", teamID);

                con.Open();
                return (int)cmd.ExecuteScalar();
            }
        }

        public DataRow GetRandomBandDefinition(int clusterID, string band)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        SELECT TOP 1 ClusterBandDefinitionID, OutputNarrative
        FROM ClusterBandDefinition
        WHERE ClusterID=@CID AND BandLevel=@Band
        ORDER BY NEWID()";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@CID", clusterID);
                cmd.Parameters.AddWithValue("@Band", band);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt.Rows.Count > 0 ? dt.Rows[0] : null;
            }
        }


        public void InsertReportCluster(int reportID, int clusterID, int rawScore, double normalizedScore, string band, int? defID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        INSERT INTO ReportCluster
        (ReportID, ClusterID, RawScore, NormalizedScore, Band, ClusterBandDefinitionID)
        VALUES (@RID, @CID, @Raw, @Norm, @Band, @DefID)";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@RID", reportID);
                cmd.Parameters.AddWithValue("@CID", clusterID);
                cmd.Parameters.AddWithValue("@Raw", rawScore);
                cmd.Parameters.AddWithValue("@Norm", normalizedScore);
                cmd.Parameters.AddWithValue("@Band", band);
                cmd.Parameters.AddWithValue("@DefID", (object)defID ?? DBNull.Value);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public DataTable GetReportClusters(int sessionID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        SELECT 
            c.ClusterName,
            rc.RawScore,
            rc.NormalizedScore,
            rc.Band,
            cbd.OutputNarrative
        FROM Report r
        INNER JOIN ReportCluster rc ON r.ReportID = rc.ReportID
        INNER JOIN Cluster c ON rc.ClusterID = c.ClusterID
        LEFT JOIN ClusterBandDefinition cbd 
            ON rc.ClusterBandDefinitionID = cbd.ClusterBandDefinitionID
        WHERE r.SessionID = @SessionID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@SessionID", sessionID);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }
        public bool ReportExists(int sessionID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "SELECT COUNT(*) FROM Report WHERE SessionID=@SID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@SID", sessionID);

                con.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }
        public int GetReportID(int sessionID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "SELECT ReportID FROM Report WHERE SessionID=@SID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@SID", sessionID);

                con.Open();
                return (int)cmd.ExecuteScalar();
            }
        }


















        public DataTable GetClusters()
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "SELECT ClusterID, ClusterName FROM Cluster";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public DataTable GetAllBandDefinitions()
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        SELECT cbd.ClusterBandDefinitionID,
               c.ClusterName,
               cbd.BandLevel,
               cbd.OutputNarrative
        FROM ClusterBandDefinition cbd
        INNER JOIN Cluster c ON cbd.ClusterID = c.ClusterID";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public void InsertBand(int clusterID, string band, string narrative)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        INSERT INTO ClusterBandDefinition (ClusterID, BandLevel, OutputNarrative)
        VALUES (@CID, @Band, @Narrative)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@CID", clusterID);
                cmd.Parameters.AddWithValue("@Band", band);
                cmd.Parameters.AddWithValue("@Narrative", narrative);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateBand(int id, string narrative)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        UPDATE ClusterBandDefinition
        SET OutputNarrative=@Narrative
        WHERE ClusterBandDefinitionID=@ID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Narrative", narrative);
                cmd.Parameters.AddWithValue("@ID", id);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public void DeleteBand(int id)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "DELETE FROM ClusterBandDefinition WHERE ClusterBandDefinitionID=@ID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ID", id);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }





    }
}