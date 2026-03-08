using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Simulation_Based_Learning.Repositories
{
    public class DecisionRepository
    {
        private readonly string _connStr;
        public DecisionRepository()
        {
            _connStr = ConfigurationManager
            .ConnectionStrings["SimDB"]
            .ConnectionString;
        }

        public DataTable GetDecisionPoints(int sceneID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"SELECT DecisionPointID, DecisionPrompt
                         FROM DecisionPoint
                         WHERE SceneID = @SceneID";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@SceneID", sceneID);

                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public void AddDecisionPoint(int sceneID, string prompt)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                // compute next display order for this scene
                string orderQuery = @"
SELECT ISNULL(MAX(DisplayOrder),0) + 1
FROM DecisionPoint
WHERE SceneID = @SceneID";

                using (var orderCmd = new SqlCommand(orderQuery, con))
                {
                    orderCmd.Parameters.AddWithValue("@SceneID", sceneID);
                    int nextOrder = (int)orderCmd.ExecuteScalar();

                    string insert = @"
INSERT INTO DecisionPoint (SceneID, DecisionPrompt, DisplayOrder)
VALUES (@SceneID, @Prompt, @Order)";

                    using (var cmd = new SqlCommand(insert, con))
                    {
                        cmd.Parameters.AddWithValue("@SceneID", sceneID);
                        cmd.Parameters.AddWithValue("@Prompt", prompt);
                        cmd.Parameters.AddWithValue("@Order", nextOrder);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        public void UpdateDecisionPoint(int decisionID, string prompt)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        UPDATE DecisionPoint
        SET DecisionPrompt = @Prompt
        WHERE DecisionPointID = @ID";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Prompt", prompt);
                cmd.Parameters.AddWithValue("@ID", decisionID);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public void DeleteDecisionPoint(int decisionID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "DELETE FROM DecisionPoint WHERE DecisionPointID=@ID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ID", decisionID);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public DataTable GetOptions(int decisionPointID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"SELECT OptionID, OptionLabel, OptionText
                         FROM [Option]
                         WHERE DecisionPointID = @ID";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@ID", decisionPointID);

                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public void AddOption(int decisionPointID, string label, string text)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                con.Open();

                string orderQuery = @"
        SELECT ISNULL(MAX(DisplayOrder),0)+1
        FROM [Option]
        WHERE DecisionPointID = @DPID";

                SqlCommand orderCmd = new SqlCommand(orderQuery, con);
                orderCmd.Parameters.AddWithValue("@DPID", decisionPointID);

                int nextOrder = (int)orderCmd.ExecuteScalar();

                string query = @"
        INSERT INTO [Option]
        (DecisionPointID, OptionLabel, OptionText, DisplayOrder)
        VALUES (@DPID, @Label, @Text, @Order)";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@DPID", decisionPointID);
                cmd.Parameters.AddWithValue("@Label", label);
                cmd.Parameters.AddWithValue("@Text", text);
                cmd.Parameters.AddWithValue("@Order", nextOrder);

                cmd.ExecuteNonQuery();
            }
        }
        public void UpdateOption(int optionID, string label, string text)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        UPDATE [Option]
        SET OptionLabel = @Label,
            OptionText = @Text
        WHERE OptionID = @ID";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Label", label);
                cmd.Parameters.AddWithValue("@Text", text);
                cmd.Parameters.AddWithValue("@ID", optionID);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public void DeleteOption(int optionID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "DELETE FROM [Option] WHERE OptionID=@ID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ID", optionID);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public void AddAttributeEffect(int optionID, int attributeID, int value)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"INSERT INTO OptionAttributeEffect
                        (OptionID, AttributeID, EffectValue)
                        VALUES (@OptionID, @AttrID, @Value)";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@OptionID", optionID);
                cmd.Parameters.AddWithValue("@AttrID", attributeID);
                cmd.Parameters.AddWithValue("@Value", value);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public void DeleteEffect(int effectID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "DELETE FROM OptionAttributeEffect WHERE EffectID=@ID";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@ID", effectID);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public DataTable GetAttributeEffects(int optionID)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        SELECT EffectID, Attribute.AttributeName, EffectValue
        FROM OptionAttributeEffect
        JOIN Attribute
        ON Attribute.AttributeID = OptionAttributeEffect.AttributeID
        WHERE OptionID = @OptionID";

                SqlDataAdapter da = new SqlDataAdapter(query, con);

                da.SelectCommand.Parameters.AddWithValue("@OptionID", optionID);

                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public DataTable GetAttributes()
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"SELECT AttributeID, AttributeName
                         FROM Attribute
                         ORDER BY AttributeName";

                SqlDataAdapter da = new SqlDataAdapter(query, con);

                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        public void AddAttribute(string name)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"INSERT INTO Attribute (AttributeName)
                         VALUES (@Name)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Name", name);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public void UpdateAttribute(int attributeID, string name)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = @"
        UPDATE Attribute
        SET AttributeName = @Name
        WHERE AttributeID = @ID";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@ID", attributeID);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public void DeleteAttribute(int id)
        {
            using (SqlConnection con = new SqlConnection(_connStr))
            {
                string query = "DELETE FROM Attribute WHERE AttributeID=@ID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ID", id);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }






    }
}