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

        // GenerateJoinCode creates a random 6-character alphanumeric string to be used as a unique join code for teams. It uses uppercase letters and digits, and ensures that the generated code is reasonably unique by combining randomness with a sufficiently large character set.
        public string GenerateJoinCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random rand = new Random();

            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[rand.Next(s.Length)]).ToArray());
        }

        // CreateSession checks if there is an existing active session for the given team. If a session with status 'Waiting' or 'InProgress' exists, it returns that session's ID. If no such session exists, it creates a new session for the specified team and simulation, sets its status to 'Waiting', and returns the new session's ID. This method ensures that teams do not have multiple concurrent sessions and can easily resume existing sessions.
        public int CreateSession(int teamID, int simulationID)
        {
            SqlCommand cmd = new SqlCommand(@"
                DECLARE @sessionID INT;

                -- Try get existing session
                SELECT TOP 1 @sessionID = SessionID
                FROM PlaythroughSession
                WHERE TeamID = @team
                AND Status IN ('Waiting','InProgress')
                ORDER BY CreatedDate DESC;

                -- If none exists, create one
                IF @sessionID IS NULL
                BEGIN
                    INSERT INTO PlaythroughSession (TeamID, SimulationID, Status)
                    VALUES (@team, @sim, 'Waiting');

                    SET @sessionID = SCOPE_IDENTITY();
                END

                SELECT @sessionID;
            ");

            cmd.Parameters.AddWithValue("@team", teamID);
            cmd.Parameters.AddWithValue("@sim", simulationID);

            return (int)ExecuteScalar(cmd);
        }

        // GetSceneBundle retrieves all the necessary information for the current scene in a playthrough session, including scene details, associated decision points, and available options. It executes a stored procedure that joins multiple tables to gather this data, returning it as a DataTable for use in the application.
        public DataTable GetSceneBundle(int sessionID)
        {
            SqlCommand cmd = new SqlCommand("GetSceneBundle");
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@SessionID", sessionID);
            return ExecuteQuery(cmd);
        }

        // SubmitDecisionAndAdvance allows a player to submit their decision for a given decision point and then checks if all players have made their decisions. If all players have decided, it advances the session to the next scene. The method returns a status string indicating whether the decision was recorded successfully and if the session has advanced.
        public string SubmitDecisionAndAdvance(int sessionID, int userID, int decisionID, int optionID)
        {
            SqlCommand cmd = new SqlCommand("SubmitDecisionAndAdvance");
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@SessionID", sessionID);
            cmd.Parameters.AddWithValue("@UserID", userID);
            cmd.Parameters.AddWithValue("@DecisionPointID", decisionID);
            cmd.Parameters.AddWithValue("@OptionID", optionID);

            DataTable dt = ExecuteQuery(cmd);

            string status = dt.Rows[0]["Status"].ToString();

            return status;
        }

        // HaveAllPlayersDecided checks if all players in the session have made their decisions for a specific decision point. It counts the number of players who have not yet submitted their decisions and returns that count. If the count is zero, it indicates that all players have decided, allowing the session to advance to the next scene.
        public int HaveAllPlayersDecided(int sessionID, int decisionID)
        {
            SqlCommand cmd = new SqlCommand(@"
            SELECT COUNT(*)
            FROM TeamMember tm
            INNER JOIN PlaythroughSession ps ON tm.TeamID = ps.TeamID
            WHERE ps.SessionID = @SessionID
            AND tm.UserID NOT IN (
                SELECT UserID FROM PlayerDecision
                WHERE SessionID = @SessionID
                AND DecisionPointID = @DecisionPointID
            )");

            cmd.Parameters.AddWithValue("@SessionID", sessionID);
            cmd.Parameters.AddWithValue("@DecisionPointID", decisionID);

            return (int)ExecuteScalar(cmd); // returns remaining players
        }

        // UpdateSessionStatus updates the status of a playthrough session (e.g., 'Waiting', 'InProgress', 'Completed') based on the provided session ID. This method is typically called when the session state changes, such as when all players have made their decisions or when the session is completed. It executes an UPDATE statement to modify the session's status in the database.
        public void UpdateSessionStatus(int sessionID, string status)
        {
            SqlCommand cmd = new SqlCommand(@"UPDATE PlaythroughSession SET Status = @Status WHERE SessionID = @SessionID");
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@SessionID", sessionID);  
            ExecuteNonQuery(cmd);
        }

        // CreateTeam creates a new team with the specified name and a generated join code. It inserts a new record into the Team table and returns the newly created TeamID. This method ensures that each team has a unique join code that players can use to join the team.
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

        // AddMember adds a user to a team by inserting a record into the TeamMember table. It checks if the user is already a member of the team to prevent duplicate entries. If the user is not already a member, it adds them to the team, allowing them to participate in sessions associated with that team.
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

        // GetTeamByJoinCode retrieves the team information based on a provided join code. It executes a SELECT query to find the team that matches the given join code and returns the team's data as a DataRow. If no team is found with the specified join code, it returns null.
        public DataRow GetTeamByJoinCode(string code)
        {
            SqlCommand cmd = new SqlCommand(
                "SELECT * FROM Team WHERE JoinCode = @code");

            cmd.Parameters.AddWithValue("@code", code);

            DataTable dt = ExecuteQuery(cmd);

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        // GetActiveSessionID checks if there is an active playthrough session for a given team. It looks for sessions that are not marked as 'Completed' and returns the most recent active session's ID. If no active session exists, it returns 0, indicating that the team can start a new session.
        public int GetActiveSessionID(int teamID)
        {
            SqlCommand cmd = new SqlCommand(@"
            SELECT TOP 1 SessionID
            FROM PlaythroughSession
            WHERE TeamID = @team
            AND Status != 'Completed'
            ORDER BY CreatedDate DESC");

            cmd.Parameters.AddWithValue("@team", teamID);

            object result = ExecuteScalar(cmd);

            return result != null ? Convert.ToInt32(result) : 0;
        }

        // SelectCharacter allows a user to select a character for a specific playthrough session. It inserts a record into the CharacterSelection table with the session ID, user ID, and chosen character name. The method returns true if the selection is successful, or false if the user has already selected a character (enforced by a unique constraint in the database).
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

        // InitializeSessionProgress sets the CurrentSceneID for a playthrough session to the first scene of the simulation. It retrieves the first scene based on the simulation's phases and their associated scenes, then updates the PlaythroughSession record with this initial scene ID. This method is typically called when a session starts to ensure that players begin at the correct starting point in the simulation.
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

        // GetPlayerStatus retrieves the total number of players in a team and the number of players who have selected their characters for a specific session. It executes a SQL query that counts the total members of the team and the entries in the CharacterSelection table for the session, returning both counts as a tuple.
        public (int totalPlayers, int selectedPlayers) GetPlayerStatus(int teamID, int sessionID)
        {
            SqlCommand cmd = new SqlCommand(@"
        SELECT 
            (SELECT COUNT(*) FROM TeamMember WHERE TeamID = @team) AS TotalPlayers,
            (SELECT COUNT(*) FROM CharacterSelection WHERE SessionID = @session) AS SelectedPlayers");

            cmd.Parameters.AddWithValue("@team", teamID);
            cmd.Parameters.AddWithValue("@session", sessionID);

            DataTable dt = ExecuteQuery(cmd);

            DataRow row = dt.Rows[0];

            return (
                Convert.ToInt32(row["TotalPlayers"]),
                Convert.ToInt32(row["SelectedPlayers"])
            );
        }

        // GetTeamLobbyData retrieves the join code for a team and a list of players in that team. It executes a SQL query that first selects the JoinCode from the Team table and then retrieves the UserID and Username of all members of the team by joining the TeamMember and User tables. The method returns a tuple containing the join code and a DataTable of players.
        public (string joinCode, DataTable players) GetTeamLobbyData(int teamID)
        {
            SqlCommand cmd = new SqlCommand(@"
                SELECT JoinCode FROM Team WHERE TeamID = @team;

                SELECT U.UserID, U.Username
                FROM TeamMember TM
                INNER JOIN [User] U ON TM.UserID = U.UserID
                WHERE TM.TeamID = @team;
            ");

            cmd.Parameters.AddWithValue("@team", teamID);

            DataSet ds = ExecuteDataSet(cmd);

            string joinCode = ds.Tables[0].Rows[0]["JoinCode"].ToString();
            DataTable players = ds.Tables[1];

            return (joinCode, players);
        }

        // GetTakenCharacters retrieves a list of character names that have already been selected by players in a specific playthrough session. It executes a SQL query to select the CharacterName from the CharacterSelection table for the given session ID and returns the results as a list of strings.
        public List<string> GetTakenCharacters(int sessionID)
        {
            SqlCommand cmd = new SqlCommand(@"SELECT CharacterName FROM CharacterSelection WHERE SessionID = @session");
            cmd.Parameters.AddWithValue("@session", sessionID);

            DataTable dt = ExecuteQuery(cmd);
            return dt.AsEnumerable().Select(r => r.Field<string>("CharacterName")).ToList();
        }
        
        // GetPlayerCharacter retrieves the character name selected by a specific user in a given playthrough session. It executes a SQL query to find the CharacterName from the CharacterSelection table based on the session ID and user ID, returning the character name as a string. If no character is found for the user, it returns an empty string.
        public string GetPlayerCharacter(int sessionID, int userID)
        {
            SqlCommand cmd = new SqlCommand(@"
            SELECT CharacterName
            FROM CharacterSelection
            WHERE SessionID = @session AND UserID = @user");

            cmd.Parameters.AddWithValue("@session", sessionID);
            cmd.Parameters.AddWithValue("@user", userID);

            object result = ExecuteScalar(cmd);

            return result?.ToString() ?? "";
        }

    }
}