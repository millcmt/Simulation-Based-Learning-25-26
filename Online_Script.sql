--
--
--
USE Simulation-Based-Learning-26; GO
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
-- USER & ROLE STRUCTURE, InteractionAudit, 
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
CREATE TABLE [User] (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255)UNIQUE,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NULL,
    Role NVARCHAR(20) CHECK (Role IN ('Admin','Player','Guest')) NOT NULL,
    TimeStamp DATETIME DEFAULT GETDATE(),
    IsGuest BIT DEFAULT 0
);

INSERT INTO [User] (Username, Email, PasswordHash, Role, IsGuest)
VALUES ('admin', 'admin@sim.com', 'admin123', 'Admin', 0);

CREATE TABLE InteractionAudit (
    AuditID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    Operation NVARCHAR(100),
    ChangeDetails NVARCHAR(MAX),
    AuditTime DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES [User](UserID)
);
GO
SELECT * FROM [User];
SELECT * FROM InteractionAudit;
GO
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
--SIMULATION STRUCTURE, PhaseTemplate,SimulationPhase, Scene, Dialogue, Team, TeamMember, PlaythroughSession,CharacterSelection
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
CREATE TABLE Simulation (
    SimulationID INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200),
    ScenarioName NVARCHAR(200),
    Description NVARCHAR(MAX),
    CreatedDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(20) CHECK (Status IN ('Active','Completed')) NOT NULL
);
CREATE TABLE PhaseTemplate (
    PhaseTemplateID INT IDENTITY PRIMARY KEY,
    PhaseTitle NVARCHAR(200) NOT NULL,
    Objective NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    VersionNumber INT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE()
);
ALTER TABLE PhaseTemplate
ADD CONSTRAINT UQ_PhaseTemplate_Title_Version UNIQUE (PhaseTitle, VersionNumber);

CREATE TABLE SimulationPhase (
    SimulationPhaseID INT IDENTITY PRIMARY KEY,
    SimulationID INT NOT NULL,
    PhaseTemplateID INT NOT NULL,
    DisplayOrder INT NOT NULL,

    FOREIGN KEY (SimulationID)REFERENCES Simulation(SimulationID)ON DELETE CASCADE,
    FOREIGN KEY (PhaseTemplateID)REFERENCES PhaseTemplate(PhaseTemplateID)
);
ALTER TABLE SimulationPhase 
ADD CONSTRAINT UQ_SimulationPhase_Order UNIQUE (SimulationID, DisplayOrder);

CREATE TABLE Scene (
    SceneID INT IDENTITY(1,1) PRIMARY KEY,
    SceneTitle NVARCHAR(200),
    DisplayOrder INT,
    PhaseTemplateID INT NOT NULL,
    VideoPath NVARCHAR(500) NULL,
    ImagePath NVARCHAR(500) NULL,
    FOREIGN KEY (PhaseID) REFERENCES Phase(PhaseID)
);
ALTER TABLE Scene
ADD CONSTRAINT FK_Scene_PhaseTemplate FOREIGN KEY (PhaseTemplateID)
REFERENCES PhaseTemplate(PhaseTemplateID) ON DELETE CASCADE;

CREATE TABLE Dialogue (
    DialogueID INT IDENTITY(1,1) PRIMARY KEY,
    SceneID INT NOT NULL,
    Speaker NVARCHAR(100),
    Dialogue NVARCHAR(MAX),
    DisplayOrder INT,
    FOREIGN KEY (SceneID) REFERENCES Scene(SceneID)
);

CREATE TABLE Team (
    TeamID INT IDENTITY(1,1) PRIMARY KEY,
    TeamName NVARCHAR(200) NOT NULL,
    SimulationID INT NOT NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    JoinCode NVARCHAR(10) UNIQUE,
    FOREIGN KEY (SimulationID)
        REFERENCES Simulation(SimulationID)
        ON DELETE CASCADE
);

CREATE TABLE TeamMember (
    TeamMemberID INT IDENTITY(1,1) PRIMARY KEY,
    TeamID INT NOT NULL,
    UserID INT NOT NULL,
    FOREIGN KEY (TeamID)
        REFERENCES Team(TeamID)
        ON DELETE CASCADE,
    FOREIGN KEY (UserID)
        REFERENCES [User](UserID)
);
ALTER TABLE TeamMember
ADD CONSTRAINT UQ_Team_User UNIQUE (TeamID, UserID);

CREATE TABLE PlaythroughSession (
    SessionID INT IDENTITY PRIMARY KEY,
    TeamID INT,
    SimulationID INT,
    CurrentPhaseID INT,
    CurrentSceneID INT,
    Status NVARCHAR(50),
    CreatedDate DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (TeamID) REFERENCES Team(TeamID),
    FOREIGN KEY (SimulationID) REFERENCES Simulation(SimulationID)
)

CREATE TABLE CharacterSelection (
    CharacterSelectionID INT IDENTITY PRIMARY KEY,
    SessionID INT,
    UserID INT,
    CharacterName NVARCHAR(100),
    FOREIGN KEY (SessionID) REFERENCES PlaythroughSession(SessionID),
    FOREIGN KEY (UserID) REFERENCES [User](UserID)
);
ALTER TABLE CharacterSelection
ADD CONSTRAINT UQ_Session_Character UNIQUE (SessionID, CharacterName);
GO
SELECT * FROM Simulation;
SELECT * FROM PhaseTemplate;
SELECT * FROM SimulationPhase;
SELECT * FROM Scene;
SELECT * FROM Dialogue;
SELECT * FROM Team;
SELECT * FROM TeamMember;
SELECT * FROM Scene;
SELECT * FROM PlaythroughSession;
SELECT * FROM CharacterSelection;
GO
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
--DECISION ENGINE, DecisionPoint, [Option], PlayerDecision, TeamDecision,
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
CREATE TABLE DecisionPoint (
    DecisionPointID INT IDENTITY(1,1) PRIMARY KEY,
    SceneID INT NOT NULL,
    DecisionPrompt NVARCHAR(MAX),
    DisplayOrder INT NOT NULL DEFAULT 1,
    FOREIGN KEY (SceneID) REFERENCES Scene(SceneID)
);
ALTER TABLE DecisionPoint
ADD CONSTRAINT UQ_Decision_Order UNIQUE (SceneID, DisplayOrder);

CREATE TABLE [Option] (
    OptionID INT IDENTITY(1,1) PRIMARY KEY,
    DecisionPointID INT NOT NULL,
    OptionLabel NVARCHAR(5),
    OptionText NVARCHAR(MAX),
    DisplayOrder INT NOT NULL DEFAULT 1,
    FOREIGN KEY (DecisionPointID) REFERENCES DecisionPoint(DecisionPointID)
);
ALTER TABLE [Option]
ADD CONSTRAINT UQ_Option_Order UNIQUE (DecisionPointID, DisplayOrder);

CREATE TABLE PlayerDecision (
    PlayerDecisionID INT IDENTITY(1,1) PRIMARY KEY,
    SessionID INT NOT NULL,
    UserID INT NOT NULL,
    DecisionPointID INT NOT NULL,
    OptionID INT NOT NULL,
    Timestamp DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (SessionID) REFERENCES PlaythroughSession(SessionID) ON DELETE CASCADE,
    FOREIGN KEY (UserID) REFERENCES [User](UserID),
    FOREIGN KEY (DecisionPointID) REFERENCES DecisionPoint(DecisionPointID),
    FOREIGN KEY (OptionID) REFERENCES [Option](OptionID)
);
ALTER TABLE PlayerDecision
ADD CONSTRAINT UQ_PlayerDecision UNIQUE (SessionID, UserID, DecisionPointID);

CREATE TABLE TeamDecision (
    TeamDecisionID INT IDENTITY(1,1) PRIMARY KEY,
    SessionID INT NOT NULL,
    DecisionPointID INT NOT NULL,
    SelectedOptionID INT NOT NULL,
    ResolutionTimestamp DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (SessionID) REFERENCES PlaythroughSession(SessionID) ON DELETE CASCADE,
    FOREIGN KEY (DecisionPointID) REFERENCES DecisionPoint(DecisionPointID),
    FOREIGN KEY (SelectedOptionID) REFERENCES [Option](OptionID)
);
GO
SELECT * FROM DecisionPoint;
SELECT * FROM [Option];
SELECT * FROM PlayerDecision;
SELECT * FROM TeamDecision;
GO
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
--ATTRIBUTE & CLUSTER ENGINE, OptionAttributeEffect, ClusterAttribute, ClusterBounds
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
CREATE TABLE Attribute (
    AttributeID INT IDENTITY(1,1) PRIMARY KEY,
    AttributeName NVARCHAR(200)
);

CREATE TABLE OptionAttributeEffect (
    EffectID INT IDENTITY(1,1) PRIMARY KEY,
    OptionID INT NOT NULL,
    AttributeID INT NOT NULL,
    EffectValue INT CHECK (EffectValue BETWEEN -3 AND 3),
    FOREIGN KEY (OptionID) REFERENCES [Option](OptionID),
    FOREIGN KEY (AttributeID) REFERENCES Attribute(AttributeID)
);

CREATE TABLE Cluster (
    ClusterID INT IDENTITY(1,1) PRIMARY KEY,
    ClusterName NVARCHAR(200)
);

CREATE TABLE ClusterAttribute (
    ClusterAttributeID INT IDENTITY(1,1) PRIMARY KEY,
    ClusterID INT NOT NULL,
    AttributeID INT NOT NULL,
    FOREIGN KEY (ClusterID) REFERENCES Cluster(ClusterID),
    FOREIGN KEY (AttributeID) REFERENCES Attribute(AttributeID)
);
CREATE TABLE ClusterBounds (
    ClusterID INT PRIMARY KEY,

    TotalOccurrences INT,   
    MaxEffect INT,          

    MinScore INT,
    MaxScore INT,
    RangeScore INT,

    LowUpper FLOAT,
    ModerateUpper FLOAT,

    FOREIGN KEY (ClusterID) REFERENCES Cluster(ClusterID)
);
GO
SELECT * FROM Attribute;
SELECT * FROM OptionAttributeEffect;
SELECT * FROM Cluster;
SELECT * FROM ClusterAttribute;
SELECT * FROM ClusterBounds;
GO
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
--REPORTING LAYER, ClusterBandDefinition, ReportCluster
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
CREATE TABLE Report (
    ReportID INT IDENTITY(1,1) PRIMARY KEY,
    SessionID INT NULL,
    GeneratedDate DATETIME DEFAULT GETDATE(),
    TeamID INT NULL,
    FOREIGN KEY (SessionID) REFERENCES PlaythroughSession(SessionID),
    FOREIGN KEY (TeamID) REFERENCES Team(TeamID) ON DELETE CASCADE
);
ALTER TABLE Report
ADD CONSTRAINT UQ_Report_Session UNIQUE (SessionID)

CREATE TABLE ClusterBandDefinition (
    ClusterBandDefinitionID INT IDENTITY(1,1) PRIMARY KEY,
    ClusterID INT NOT NULL,
    BandLevel NVARCHAR(20) CHECK (BandLevel IN ('Low','Moderate','High')),
    OutputNarrative NVARCHAR(MAX),
    FOREIGN KEY (ClusterID) REFERENCES Cluster(ClusterID)
);

CREATE TABLE ReportCluster (
    ReportClusterScoreID INT IDENTITY(1,1) PRIMARY KEY,
    ReportID INT NOT NULL,
    ClusterID INT NOT NULL,
    RawScore INT,
    NormalizedScore FLOAT,
    Band NVARCHAR(20),
    ClusterBandDefinitionID INT,
    FOREIGN KEY (ReportID) REFERENCES Report(ReportID),
    FOREIGN KEY (ClusterID) REFERENCES Cluster(ClusterID),
    FOREIGN KEY (ClusterBandDefinitionID) REFERENCES ClusterBandDefinition(ClusterBandDefinitionID)
);
GO
SELECT * FROM Report;
SELECT * FROM ClusterBandDefinition;
SELECT * FROM ReportCluster;
GO
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
-- STORED PROCEDURES, GetSceneBundle
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
CREATE PROCEDURE GetSceneBundle
    @SessionID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        s.SceneID,
        s.SceneTitle,
        s.VideoPath,
        s.ImagePath,

        d.Dialogue,
        d.Speaker,
        d.DisplayOrder AS DialogueOrder,

        dp.DecisionPointID,
        dp.DecisionPrompt,

        o.OptionID,
        o.OptionText

    FROM PlaythroughSession ps

    INNER JOIN Scene s 
        ON ps.CurrentSceneID = s.SceneID

    LEFT JOIN Dialogue d 
        ON s.SceneID = d.SceneID

    LEFT JOIN DecisionPoint dp 
        ON s.SceneID = dp.SceneID

    LEFT JOIN [Option] o 
        ON dp.DecisionPointID = o.DecisionPointID

    WHERE ps.SessionID = @SessionID

    ORDER BY d.DisplayOrder, o.OptionID
END GO
GO
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
CREATE PROCEDURE SubmitDecisionAndAdvance
    @SessionID INT,
    @UserID INT,
    @DecisionPointID INT,
    @OptionID INT
AS
BEGIN
    SET NOCOUNT ON;

    --Insert decision
    IF NOT EXISTS (
        SELECT 1 FROM PlayerDecision
        WHERE SessionID = @SessionID
        AND UserID = @UserID
        AND DecisionPointID = @DecisionPointID
    )
    BEGIN
        INSERT INTO PlayerDecision (SessionID, UserID, DecisionPointID, OptionID)
        VALUES (@SessionID, @UserID, @DecisionPointID, @OptionID)
    END

    --  Check if all submitted
    DECLARE @Remaining INT

    SELECT @Remaining = COUNT(*)
    FROM TeamMember tm
    INNER JOIN PlaythroughSession ps ON tm.TeamID = ps.TeamID
    WHERE ps.SessionID = @SessionID
    AND tm.UserID NOT IN (
        SELECT UserID FROM PlayerDecision
        WHERE SessionID = @SessionID
        AND DecisionPointID = @DecisionPointID
    )

    IF @Remaining > 0
    BEGIN
        SELECT 'WAITING' AS Status
        RETURN
    END

    --  CREATE TEAM DECISION (majority + tie-breaker)
    IF NOT EXISTS (
        SELECT 1 FROM TeamDecision
        WHERE SessionID = @SessionID
        AND DecisionPointID = @DecisionPointID
    )
    BEGIN
        ;WITH VoteCounts AS (
            SELECT 
                OptionID,
                COUNT(*) AS VoteCount,
                MIN([Timestamp]) AS FirstPick
            FROM PlayerDecision
            WHERE SessionID = @SessionID
            AND DecisionPointID = @DecisionPointID
            GROUP BY OptionID
        )
        INSERT INTO TeamDecision (SessionID, DecisionPointID, SelectedOptionID)
        SELECT TOP 1 @SessionID, @DecisionPointID, OptionID
        FROM VoteCounts
        ORDER BY VoteCount DESC, FirstPick ASC
    END

    --  Get decision result (for UI)
    DECLARE @SelectedOptionID INT

    SELECT TOP 1 @SelectedOptionID = SelectedOptionID
    FROM TeamDecision
    WHERE SessionID = @SessionID
    AND DecisionPointID = @DecisionPointID

    --  Determine next scene
    DECLARE @NextSceneID INT
    DECLARE @CurrentSceneID INT

    SELECT TOP 1 @CurrentSceneID = CurrentSceneID 
    FROM PlaythroughSession 
    WHERE SessionID = @SessionID

    -- Same phase
    SELECT TOP 1 @NextSceneID = s2.SceneID
    FROM Scene s1
    INNER JOIN Scene s2 
        ON s1.PhaseTemplateID = s2.PhaseTemplateID
    WHERE s1.SceneID = @CurrentSceneID
    AND s2.DisplayOrder > s1.DisplayOrder
    ORDER BY s2.DisplayOrder


    -- Get current phase order
    DECLARE @CurrentPhaseOrder INT;

    SELECT TOP 1 @CurrentPhaseOrder = sp.DisplayOrder
    FROM Scene s
    INNER JOIN SimulationPhase sp 
        ON s.PhaseTemplateID = sp.PhaseTemplateID
    WHERE s.SceneID = @CurrentSceneID;

    -- Next phase
    IF @NextSceneID IS NULL
    BEGIN
        SELECT TOP 1 @NextSceneID = s.SceneID
        FROM Scene s
        INNER JOIN SimulationPhase sp ON s.PhaseTemplateID = sp.PhaseTemplateID
        WHERE sp.SimulationID = (
            SELECT TOP 1 SimulationID 
            FROM PlaythroughSession 
            WHERE SessionID = @SessionID
       
        )
        AND sp.DisplayOrder > @CurrentPhaseOrder
    ORDER BY sp.DisplayOrder, s.DisplayOrder
    END

    --  HANDLE COMPLETION OR ADVANCE
    IF @NextSceneID IS NULL
    BEGIN
        UPDATE PlaythroughSession
        SET Status = 'Completed'
        WHERE SessionID = @SessionID

        SELECT 
            'COMPLETED' AS Status,
            o.OptionText,
            a.AttributeName,
            oae.EffectValue
        FROM [Option] o
        LEFT JOIN OptionAttributeEffect oae ON o.OptionID = oae.OptionID
        LEFT JOIN Attribute a ON oae.AttributeID = a.AttributeID
        WHERE o.OptionID = @SelectedOptionID

        RETURN
    END
    ELSE
    BEGIN
        -- ADVANCE NOW (same SP like you want)
        UPDATE PlaythroughSession
        SET CurrentSceneID = @NextSceneID
        WHERE SessionID = @SessionID

        SELECT 
            'RESOLVED' AS Status,
            o.OptionText,
            a.AttributeName,
            oae.EffectValue
        FROM [Option] o
        LEFT JOIN OptionAttributeEffect oae ON o.OptionID = oae.OptionID
        LEFT JOIN Attribute a ON oae.AttributeID = a.AttributeID
        WHERE o.OptionID = @SelectedOptionID

        RETURN
    END
END
GO
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--







