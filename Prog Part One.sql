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

INSERT INTO dbo.Organiser (Name, Email, Phone) VALUES
('Johannesburg Road Runners Club', 'events@joburgroadrunners.co.za', '0114567890'),
('Cape Peninsula Cycling Association', 'races@cpca.org.za', '0219876543');

INSERT INTO dbo.Participant (Name, Email, Phone, DateOfBirth, Gender) VALUES
('Lethabo Mokoena', 'lethabo.mokoena@example.com', '0731234567', '2001-04-12', 'Male'),
('Naledi Dlamini', 'naledi.dlamini@example.com', '0827654321', '1998-09-23', 'Female'); 

INSERT INTO dbo.Event (OrganiserID, Name, Description, EventDate, Location, DistanceKm, EventType) VALUES
(1, 'Joburg Winter 10K', 'Annual community 10km road run through Emmarentia and surrounds.', '2026-06-14', 'Emmarentia, Johannesburg', 10.00, 'Run'),
(1, 'Soweto Heritage Walk', 'Charity walk celebrating local heritage sites in Soweto.', '2026-09-24', 'Soweto, Johannesburg', 5.00, 'Walk'),
(2, 'Cape Peninsula Cycle Classic', 'Scenic road cycling event around the Cape Peninsula.', '2026-11-08', 'Simon''s Town, Cape Town', 60.00, 'Cycle');

INSERT INTO dbo.Category (EventID, Name, DistanceKm, MinAge, MaxAge, Gender) VALUES
(1, '10km Open', 10.00, 16, NULL, 'Any'),
(1, '10km Senior (50+)', 10.00, 50, NULL, 'Any'),
(2, '5km Family Walk', 5.00, NULL, NULL, 'Any'),
(3, '60km Open', 60.00, 18, NULL, 'Any'),
(3, '60km Under 20', 60.00, NULL, 19, 'Any');

INSERT INTO dbo.Route (EventID, Name, DistanceKm, ElevationProfile, MapURL) VALUES
(1, 'Emmarentia Loop', 10.00, 'Mostly flat with one moderate incline at the 6km mark.', 'https://maps.example.com/routes/joburg-winter-10k'),
(2, 'Soweto Heritage Trail', 5.00, 'Flat, paved route through residential streets.', 'https://maps.example.com/routes/soweto-heritage-walk'),
(3, 'Peninsula Coastal Loop', 60.00, 'Rolling hills with a steep climb near Chapman''s Peak.', 'https://maps.example.com/routes/cape-peninsula-cycle-classic');

INSERT INTO dbo.Registration (ParticipantID, CategoryID, BibNumber, RegistrationDate) VALUES
(1, 1, 'B1001', '2026-05-01T09:00:00'), -- Lethabo -> Joburg 10km Open
(2, 1, 'B1002', '2026-05-02T10:15:00'), -- Naledi -> Joburg 10km Open
(1, 4, 'B2001', '2026-10-01T08:30:00'), -- Lethabo -> Cape Peninsula 60km Open
(2, 3, 'B3001', '2026-09-10T14:00:00'); -- Naledi -> Soweto 5km Family Walk

INSERT INTO dbo.Result (RegistrationID, FinishTime, OverallRank, Status) VALUES
(1, '00:52:34', 14, 'Finished'),
(2, '00:58:10', 27, 'Finished'), 
(4, '01:10:05', 8, 'Finished');

SELECT * FROM dbo.Organiser;
SELECT * FROM dbo.Participant;
SELECT * FROM dbo.Event;
SELECT * FROM dbo.Category;
SELECT * FROM dbo.Route;
SELECT * FROM dbo.Registration;
SELECT * FROM dbo.Result;




