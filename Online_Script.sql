USE Simulation-Based-Learning-26;
GO
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
ALTER TABLE TeamMember
ADD CONSTRAINT UQ_Team_User UNIQUE (TeamID, UserID);

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
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
--ATTRIBUTE & CLUSTER ENGINE, OptionAttributeEffect, ClusterAttribute
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
GO
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
--REPORTING LAYER, ClusterBandDefinition, ReportCluster
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--
CREATE TABLE Report (
    ReportID INT IDENTITY(1,1) PRIMARY KEY,
    SimulationID INT NOT NULL,
    GeneratedDate DATETIME DEFAULT GETDATE(),
    TeamID INT NULL,
    FOREIGN KEY (SimulationID) REFERENCES Simulation(SimulationID)
);
ALTER TABLE Report ADD CONSTRAINT FK_Report_Team
FOREIGN KEY (TeamID) REFERENCES Team(TeamID) ON DELETE CASCADE;

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
    Band NVARCHAR(20),
    ClusterBandDefinitionID INT,
    FOREIGN KEY (ReportID) REFERENCES Report(ReportID),
    FOREIGN KEY (ClusterID) REFERENCES Cluster(ClusterID),
    FOREIGN KEY (ClusterBandDefinitionID) REFERENCES ClusterBandDefinition(ClusterBandDefinitionID)
);
GO
--*****************************************************************************************************************************************--
--*****************************************************************************************************************************************--


