using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Simulation_Based_Learning.Repositories
{
    public class DecisionRepository : BaseRepository
    {
        //Get all decision points for a scene
        public DataTable GetDecisionPoints(int sceneID)
        {
            SqlCommand cmd = new SqlCommand(@"
            SELECT DecisionPointID, DecisionPrompt
            FROM DecisionPoint
            WHERE SceneID = @SceneID");

            cmd.Parameters.AddWithValue("@SceneID", sceneID);

            return ExecuteQuery(cmd);
        }

        //Add a new decision point to a scene, automatically assigning the next display order
        public void AddDecisionPoint(int sceneID, string prompt)
        {
            SqlCommand cmd = new SqlCommand(@"
            INSERT INTO DecisionPoint (SceneID, DecisionPrompt, DisplayOrder)
            SELECT 
                @SceneID, 
                @Prompt, 
                ISNULL(MAX(DisplayOrder),0) + 1
            FROM DecisionPoint
            WHERE SceneID = @SceneID");

            cmd.Parameters.AddWithValue("@SceneID", sceneID);
            cmd.Parameters.AddWithValue("@Prompt", prompt);

            ExecuteNonQuery(cmd);
        }

        //Add a new option to a decision point, automatically assigning the next display order
        public void AddOption(int decisionPointID, string label, string text)
        {
            SqlCommand cmd = new SqlCommand(@"
            INSERT INTO [Option] (DecisionPointID, OptionLabel, OptionText, DisplayOrder)
            SELECT 
                @DPID, 
                @Label, 
                @Text, 
                ISNULL(MAX(DisplayOrder),0) + 1
            FROM [Option]
            WHERE DecisionPointID = @DPID");

            cmd.Parameters.AddWithValue("@DPID", decisionPointID);
            cmd.Parameters.AddWithValue("@Label", label);
            cmd.Parameters.AddWithValue("@Text", text);

            ExecuteNonQuery(cmd);
        }

        //Update the prompt text of a decision point
        public void UpdateDecisionPoint(int decisionID, string prompt)
        {
            SqlCommand cmd = new SqlCommand(@"
            UPDATE DecisionPoint
            SET DecisionPrompt = @Prompt
            WHERE DecisionPointID = @ID");

            cmd.Parameters.AddWithValue("@Prompt", prompt);
            cmd.Parameters.AddWithValue("@ID", decisionID);

            ExecuteNonQuery(cmd);
        }

        //Delete an option and all associated attribute effects
        public void DeleteOption(int optionID)
        {
            SqlCommand cmd = new SqlCommand(@"
        DELETE FROM [Option] WHERE OptionID = @ID");

            cmd.Parameters.AddWithValue("@ID", optionID);

            ExecuteNonQuery(cmd);
        }

        //Get all options for a decision point
        public DataTable GetOptions(int decisionPointID)
        {
            SqlCommand cmd = new SqlCommand(@"
        SELECT OptionID, OptionLabel, OptionText
        FROM [Option]
        WHERE DecisionPointID = @ID");

            cmd.Parameters.AddWithValue("@ID", decisionPointID);

            return ExecuteQuery(cmd);
        }

        //Get all attribute effects for an option, including the attribute name for display purposes
        public DataTable GetAttributeEffects(int optionID)
        {
            SqlCommand cmd = new SqlCommand(@"
        SELECT EffectID, Attribute.AttributeName, EffectValue
        FROM OptionAttributeEffect
        JOIN Attribute
            ON Attribute.AttributeID = OptionAttributeEffect.AttributeID
        WHERE OptionID = @OptionID");

            cmd.Parameters.AddWithValue("@OptionID", optionID);

            return ExecuteQuery(cmd);
        }

        //Get all attributes for dropdown population when adding/editing effects
        public DataTable GetAttributes()
        {
            SqlCommand cmd = new SqlCommand(@"
        SELECT AttributeID, AttributeName
        FROM Attribute
        ORDER BY AttributeName");

            return ExecuteQuery(cmd);
        }

        //Add a new attribute effect to an option
        public void AddAttributeEffect(int optionID, int attributeID, int value)
        {
            SqlCommand cmd = new SqlCommand(@"
        INSERT INTO OptionAttributeEffect
        (OptionID, AttributeID, EffectValue)
        VALUES (@OptionID, @AttrID, @Value)");

            cmd.Parameters.AddWithValue("@OptionID", optionID);
            cmd.Parameters.AddWithValue("@AttrID", attributeID);
            cmd.Parameters.AddWithValue("@Value", value);

            ExecuteNonQuery(cmd);
        }

        //Delete a decision point and all associated options and effects (cascading delete)
        public void DeleteDecisionPoint(int decisionID)
        {
            SqlCommand cmd = new SqlCommand(@"
        DELETE FROM DecisionPoint 
        WHERE DecisionPointID = @ID");

            cmd.Parameters.AddWithValue("@ID", decisionID);

            ExecuteNonQuery(cmd);
        }

        //Update the label and text of an option
        public void UpdateOption(int optionID, string label, string text)
        {
            SqlCommand cmd = new SqlCommand(@"
        UPDATE [Option]
        SET OptionLabel = @Label,
            OptionText = @Text
        WHERE OptionID = @ID");

            cmd.Parameters.AddWithValue("@Label", label);
            cmd.Parameters.AddWithValue("@Text", text);
            cmd.Parameters.AddWithValue("@ID", optionID);

            ExecuteNonQuery(cmd);
        }

        //Delete an attribute effect
        public void DeleteEffect(int effectID)
        {
            SqlCommand cmd = new SqlCommand(@"
        DELETE FROM OptionAttributeEffect 
        WHERE EffectID = @ID");

            cmd.Parameters.AddWithValue("@ID", effectID);

            ExecuteNonQuery(cmd);
        }

        //Add a new attribute to the system
        public void AddAttribute(string name)
        {
            SqlCommand cmd = new SqlCommand(@"
        INSERT INTO Attribute (AttributeName)
        VALUES (@Name)");

            cmd.Parameters.AddWithValue("@Name", name);

            ExecuteNonQuery(cmd);
        }

        //Update the name of an attribute
        public void UpdateAttribute(int attributeID, string name)
        {
            SqlCommand cmd = new SqlCommand(@"
        UPDATE Attribute
        SET AttributeName = @Name
        WHERE AttributeID = @ID");

            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@ID", attributeID);

            ExecuteNonQuery(cmd);
        }

        //Delete an attribute and all associated effects (cascading delete)
        public void DeleteAttribute(int id)
        {
            SqlCommand cmd = new SqlCommand(@"
        DELETE FROM Attribute 
        WHERE AttributeID = @ID");

            cmd.Parameters.AddWithValue("@ID", id);

            ExecuteNonQuery(cmd);
        }

    }
}