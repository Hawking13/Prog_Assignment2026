CREATE DATABASE RaceDayDB;
CREATE TABLE dbo.Organiser (
OrganiserID INT IDENTITY(1,1) NOT NULL,
Name NVARCHAR(100) NOT NULL,
Email NVARCHAR(150) NOT NULL,
Phone NVARCHAR(20) NULL,
CONSTRAINT PK_Organiser PRIMARY KEY (OrganiserID),
CONSTRAINT UQ_Organiser_Email UNIQUE (Email) );

CREATE TABLE dbo.Participant (
ParticipantID INT IDENTITY(1,1) NOT NULL,
Name NVARCHAR(100) NOT NULL,
Email NVARCHAR(150) NOT NULL,
Phone NVARCHAR(20) NULL,
DateOfBirth DATE NOT NULL,
Gender NVARCHAR(10) NOT NULL,
CONSTRAINT PK_Participant PRIMARY KEY (ParticipantID),
CONSTRAINT UQ_Participant_Email UNIQUE (Email),
CONSTRAINT CK_Participant_Gender CHECK (Gender IN ('Male', 'Female', 'Other')) );

CREATE TABLE dbo.Event (
EventID INT IDENTITY(1,1) NOT NULL,
OrganiserID INT NOT NULL,
Name NVARCHAR(150) NOT NULL,
Description NVARCHAR(MAX) NULL,
EventDate DATE NOT NULL,
Location NVARCHAR(150) NOT NULL,
DistanceKm DECIMAL(6,2) NOT NULL,
EventType NVARCHAR(10) NOT NULL,
CONSTRAINT PK_Event PRIMARY KEY (EventID),
CONSTRAINT FK_Event_Organiser FOREIGN KEY (OrganiserID)
REFERENCES dbo.Organiser (OrganiserID)
ON DELETE CASCADE,
CONSTRAINT CK_Event_Type CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
CONSTRAINT CK_Event_Distance CHECK (DistanceKm > 0) );

CREATE TABLE dbo.Category (
CategoryID INT IDENTITY(1,1) NOT NULL,
EventID INT NOT NULL,
Name NVARCHAR(50) NOT NULL,
DistanceKm DECIMAL(6,2) NOT NULL,
MinAge INT NULL,
MaxAge INT NULL,
Gender NVARCHAR(10) NOT NULL DEFAULT 'Any',
CONSTRAINT PK_Category PRIMARY KEY (CategoryID),
CONSTRAINT FK_Category_Event FOREIGN KEY (EventID)
REFERENCES dbo.Event (EventID)
ON DELETE CASCADE,
CONSTRAINT UQ_Category_Event_Name UNIQUE (EventID, Name),
CONSTRAINT CK_Category_Gender CHECK (Gender IN ('Male', 'Female', 'Any')),
CONSTRAINT CK_Category_AgeRange CHECK (MinAge IS NULL OR MaxAge IS NULL OR MinAge <= MaxAge) );

CREATE TABLE dbo.Route (
RouteID INT IDENTITY(1,1) NOT NULL,
EventID INT NOT NULL,
Name NVARCHAR(100) NOT NULL,
DistanceKm DECIMAL(6,2) NOT NULL,
ElevationProfile NVARCHAR(MAX) NULL,
MapURL NVARCHAR(255) NULL,
CONSTRAINT PK_Route PRIMARY KEY (RouteID),
CONSTRAINT FK_Route_Event FOREIGN KEY (EventID)
REFERENCES dbo.Event (EventID)
ON DELETE CASCADE );

CREATE TABLE dbo.Registration (
RegistrationID INT IDENTITY(1,1) NOT NULL,
ParticipantID INT NOT NULL,
CategoryID INT NOT NULL,
BibNumber NVARCHAR(10) NOT NULL,
RegistrationDate DATETIME NOT NULL DEFAULT GETDATE(),
CONSTRAINT PK_Registration PRIMARY KEY (RegistrationID),
CONSTRAINT FK_Registration_Participant FOREIGN KEY (ParticipantID)
REFERENCES dbo.Participant (ParticipantID)
ON DELETE CASCADE,
CONSTRAINT FK_Registration_Category FOREIGN KEY (CategoryID)
REFERENCES dbo.Category (CategoryID)
ON DELETE NO ACTION,
CONSTRAINT UQ_Registration_Participant_Category UNIQUE (ParticipantID, CategoryID),
CONSTRAINT UQ_Registration_Bib UNIQUE (BibNumber) );

CREATE TABLE dbo.Result (
ResultID INT IDENTITY(1,1) NOT NULL,
RegistrationID INT NOT NULL,
FinishTime TIME(0) NULL,
OverallRank INT NULL,
Status NVARCHAR(20) NOT NULL DEFAULT 'Registered',
CONSTRAINT PK_Result PRIMARY KEY (ResultID),
CONSTRAINT FK_Result_Registration FOREIGN KEY (RegistrationID)
REFERENCES dbo.Registration (RegistrationID)
ON DELETE CASCADE,
CONSTRAINT UQ_Result_Registration UNIQUE (RegistrationID),
CONSTRAINT CK_Result_Status CHECK (Status IN ('Registered', 'Finished', 'DNF', 'DSQ')) );



