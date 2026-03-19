using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Simulation_Based_Learning.Repositories
{
    public class PlaythroughRepository : BaseRepository
    {
        

        public string GenerateJoinCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random rand = new Random();

            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[rand.Next(s.Length)]).ToArray());
        }

        public int CreateSession(int teamID, int simulationID)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("INSERT INTO PlaythroughSession (TeamID, SimulationID, Status) OUTPUT INSERTED.SessionID VALUES (@TeamID, @SimulationID, 'InProgress')", conn))
            {
                cmd.Parameters.AddWithValue("@TeamID", teamID);
                cmd.Parameters.AddWithValue("@SimulationID", simulationID);

                conn.Open();
                int sessionID = (int)cmd.ExecuteScalar();
                return sessionID;
            }
        }
        public void UpdateSessionStatus(int sessionID, string status)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("UPDATE PlaythroughSession SET Status = @Status WHERE SessionID = @SessionID", conn))
            {
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@SessionID", sessionID);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public bool CanAutoStart(int teamID, int requiredPlayers)
        {
            SqlCommand cmd = new SqlCommand(@"
        SELECT COUNT(*) FROM TeamMember WHERE TeamID = @team");

            cmd.Parameters.AddWithValue("@team", teamID);

            int count = (int)ExecuteScalar(cmd);

            return count >= requiredPlayers;
        }

        public bool IsSessionActive(int teamID)
        {
            SqlCommand cmd = new SqlCommand(@"
        SELECT COUNT(*) 
        FROM PlaythroughSession 
        WHERE TeamID = @team AND Status = 'InProgress'");

            cmd.Parameters.AddWithValue("@team", teamID);

            int count = (int)ExecuteScalar(cmd);

            return count > 0;
        }

        public bool SelectCharacter(int sessionID, int userID, string character)
{
    try
    {
        SqlCommand cmd = new SqlCommand(@"
            INSERT INTO CharacterSelection 
            (SessionID, UserID, CharacterName)
            VALUES (@session, @user, @char)");

        cmd.Parameters.AddWithValue("@session", sessionID);
        cmd.Parameters.AddWithValue("@user", userID);
        cmd.Parameters.AddWithValue("@char", character);

        ExecuteNonQuery(cmd);

        return true;
    }
    catch (SqlException ex)
    {
        if (ex.Number == 2627) return false;
        throw;
    }
}
        public int CreateTeam(string teamName)
        {
            SqlCommand cmd = new SqlCommand(@"
        INSERT INTO Team (TeamName, JoinCode)
        OUTPUT INSERTED.TeamID
        VALUES (@name, @code)");

            cmd.Parameters.AddWithValue("@name", teamName);
            cmd.Parameters.AddWithValue("@code", GenerateJoinCode());

            return (int)ExecuteScalar(cmd);
        }

        public int GetPlayerCount(int teamID)
        {
            SqlCommand cmd = new SqlCommand(@"SELECT COUNT(*) FROM TeamMember WHERE TeamID = @team");
            cmd.Parameters.AddWithValue("@team", teamID);
            return (int)ExecuteScalar(cmd);
        }

        public int GetSelectedCharacterCount(int sessionID)
        {
            SqlCommand cmd = new SqlCommand(@"SELECT COUNT(*) FROM CharacterSelection WHERE SessionID = @session");
            cmd.Parameters.AddWithValue("@session", sessionID);
            return (int)ExecuteScalar(cmd);
        }

        public List<string> GetTakenCharacters(int sessionID)
        {
            SqlCommand cmd = new SqlCommand(@"SELECT CharacterName FROM CharacterSelection WHERE SessionID = @session");
            cmd.Parameters.AddWithValue("@session", sessionID);

            DataTable dt = ExecuteQuery(cmd);
            return dt.AsEnumerable().Select(r => r.Field<string>("CharacterName")).ToList();
        }

        
        public void InitializeSessionProgress(int sessionID)
        {
            string query = @"
    SELECT TOP 1 s.SceneID
    FROM PlaythroughSession ps
    INNER JOIN SimulationPhase sp ON ps.SimulationID = sp.SimulationID
    INNER JOIN Scene s ON sp.PhaseTemplateID = s.PhaseTemplateID
    WHERE ps.SessionID = @session
    ORDER BY sp.DisplayOrder, s.DisplayOrder";

            SqlCommand cmd = new SqlCommand(query);
            cmd.Parameters.AddWithValue("@session", sessionID);

            object result = ExecuteScalar(cmd);

            if (result != null)
            {
                SqlCommand update = new SqlCommand(@"
        UPDATE PlaythroughSession
        SET CurrentSceneID = @scene
        WHERE SessionID = @session");

                update.Parameters.AddWithValue("@scene", (int)result);
                update.Parameters.AddWithValue("@session", sessionID);

                ExecuteNonQuery(update);
            }
        }
        
        public DataRow GetCurrentScene(int sessionID)
        {
            string query = @"
    SELECT s.*
    FROM PlaythroughSession ps
    INNER JOIN Scene s ON ps.CurrentSceneID = s.SceneID
    WHERE ps.SessionID = @session";

            SqlCommand cmd = new SqlCommand(query);
            cmd.Parameters.AddWithValue("@session", sessionID);

            DataTable dt = ExecuteQuery(cmd);

            if (dt.Rows.Count > 0)
                return dt.Rows[0];

            return null;
        }
        public string GetSessionStatus(int sessionID)
        {
            SqlCommand cmd = new SqlCommand(@"
        SELECT Status FROM PlaythroughSession WHERE SessionID = @id");

            cmd.Parameters.AddWithValue("@id", sessionID);

            return ExecuteScalar(cmd)?.ToString();
        }
        public int GetNextSceneID(int sessionID, int currentSceneID)
        {
            // 1. Try next scene in SAME phase
            string samePhaseQuery = @"
        SELECT TOP 1 s2.SceneID
        FROM Scene s1
        INNER JOIN Scene s2 ON s1.PhaseTemplateID = s2.PhaseTemplateID
        WHERE s1.SceneID = @currentScene
          AND s2.DisplayOrder > s1.DisplayOrder
        ORDER BY s2.DisplayOrder";

            SqlCommand cmdSame = new SqlCommand(samePhaseQuery);
            cmdSame.Parameters.AddWithValue("@currentScene", currentSceneID);

            object samePhaseResult = ExecuteScalar(cmdSame);

            if (samePhaseResult != null)
                return (int)samePhaseResult;

            // 2. No more scenes in phase → go to NEXT phase
            string nextPhaseQuery = @"
        SELECT TOP 1 s.SceneID
        FROM Scene s
        INNER JOIN SimulationPhase sp ON s.PhaseTemplateID = sp.PhaseTemplateID
        WHERE sp.SimulationID = (
            SELECT SimulationID FROM PlaythroughSession WHERE SessionID = @session
        )
                AND sp.DisplayOrder > (
            SELECT MIN(sp2.DisplayOrder)
            FROM Scene s2
            INNER JOIN SimulationPhase sp2 
                ON s2.PhaseTemplateID = sp2.PhaseTemplateID
            WHERE s2.SceneID = @currentScene
        )
        ORDER BY sp.DisplayOrder, s.DisplayOrder";

            SqlCommand cmdNextPhase = new SqlCommand(nextPhaseQuery);
            cmdNextPhase.Parameters.AddWithValue("@session", sessionID);
            cmdNextPhase.Parameters.AddWithValue("@currentScene", currentSceneID);

            object nextPhaseResult = ExecuteScalar(cmdNextPhase);

            if (nextPhaseResult != null)
                return (int)nextPhaseResult;

            // 3. No more phases → simulation finished
            return 0;
        }

        public void SetCurrentScene(int sessionID, int sceneID)
        {
            SqlCommand cmd = new SqlCommand(@"
        UPDATE PlaythroughSession
        SET CurrentSceneID = @scene
        WHERE SessionID = @session");

            cmd.Parameters.AddWithValue("@scene", sceneID);
            cmd.Parameters.AddWithValue("@session", sessionID);

            ExecuteNonQuery(cmd);
        }
        public bool HaveAllPlayersDecided(int sessionID, int decisionID)
        {
            // Get all players in this session's team
            SqlCommand cmdPlayers = new SqlCommand(@"
        SELECT COUNT(*) 
        FROM TeamMember tm
        INNER JOIN PlaythroughSession ps ON tm.TeamID = ps.TeamID
        WHERE ps.SessionID = @session");
            cmdPlayers.Parameters.AddWithValue("@session", sessionID);
            int totalPlayers = (int)ExecuteScalar(cmdPlayers);

            // Get count of players who have submitted this decision
            SqlCommand cmdDecisions = new SqlCommand(@"
        SELECT COUNT(DISTINCT PlayerID) 
        FROM PlayerDecision
        WHERE SessionID = @session AND DecisionPointID = @decision");
            cmdDecisions.Parameters.AddWithValue("@session", sessionID);
            cmdDecisions.Parameters.AddWithValue("@decision", decisionID);
            int submittedCount = (int)ExecuteScalar(cmdDecisions);

            return totalPlayers == submittedCount;
        }
        public bool SubmitPlayerDecision(int sessionID, int playerID, int decisionID, int optionID)
        {
            // Check if this player already submitted for this decision
            SqlCommand cmdCheck = new SqlCommand(@"
        SELECT COUNT(*) FROM PlayerDecision 
        WHERE SessionID=@session AND PlayerID=@player AND DecisionPointID=@decision");
            cmdCheck.Parameters.AddWithValue("@session", sessionID);
            cmdCheck.Parameters.AddWithValue("@player", playerID);
            cmdCheck.Parameters.AddWithValue("@decision", decisionID);

            int alreadySubmitted = (int)ExecuteScalar(cmdCheck);

            if (alreadySubmitted == 0)
            {
                // Insert the player's decision
                SqlCommand cmdInsert = new SqlCommand(@"
            INSERT INTO PlayerDecision (SessionID, PlayerID, DecisionPointID, OptionID)
            VALUES (@session, @player, @decision, @option)");
                cmdInsert.Parameters.AddWithValue("@session", sessionID);
                cmdInsert.Parameters.AddWithValue("@player", playerID);
                cmdInsert.Parameters.AddWithValue("@decision", decisionID);
                cmdInsert.Parameters.AddWithValue("@option", optionID);

                ExecuteNonQuery(cmdInsert);
                return true;
            }

            return false; // Already submitted
        }


    }
}