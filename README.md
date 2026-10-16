# Prog_Assignment2026
All files are here
The ERD and Sql Code are also in the PDF
YouTube Link: https://youtu.be/DCly89WCv1o

RaceDay – Part 2: RESTful API

YouTube Link: https://youtu.be/PvUmwVofXNs

RaceDay is an event management API for South African road running, walking and cycling events. Organisers create and manage events, categories and results. Participants browse events, enrol, and view their own results.
Built with ASP.NET Core Web API, Entity Framework Core (Code-First), SQL Server, session-based authentication, Swagger, xUnit and GitHub Actions.

Roles
Role	What they can do
Organiser	Register/log in, view and update own profile, create/update/delete their own events, create/update/delete categories for their events, view all enrolments for their events, capture and correct results.
Participant	Register/log in, view and update own profile, browse events and categories, enrol in an event by choosing a category, withdraw from an enrolment, view their own enrolments and results.
Both roles can browse events and categories. Organiser actions are blocked for Participants (and the reverse) with 403 Forbidden. Unauthenticated requests to protected endpoints get 401 Unauthorized. Organisers can only change events they own.

Setup and run
Prerequisites
Visual Studio 2022 (ASP.NET workload) and the .NET SDK matching the project (see <TargetFramework> in RaceDay.Api.csproj)
SQL Server / SQL Server Express and SSMS
Steps
Clone the repository and open the solution in Visual Studio.
Open RaceDay.Api/appsettings.json and set the connection string for your SQL Server instance:
   "ConnectionStrings": { "RaceDayDb": "Server=YOUR_SERVER;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True" }
   
Create the database. In Package Manager Console (Default project: RaceDay.Api):
   Update-Database

Set RaceDay.Api as the startup project and press F5. 
Open https://localhost:<port>/swagger to view and test every endpoint.

Using Swagger
POST /api/auth/register – create an Organiser (or a Participant, which also needs dateOfBirth and gender).
POST /api/auth/login – a session cookie is stored by the browser and sent automatically on later requests.
Call any endpoint. Use POST /api/auth/logout to switch user.

Run the unit tests
Visual Studio: Test → Run All Tests
Command line: dotnet test
The tests start the API in memory with a temporary in-memory database, so they do not touch the SQL Server database.

Project structure
RaceDay.Api/
  Controllers/   Auth, Profile, Events, Categories, Enrolments, Results
  Models/        Entity classes (one per table in the ERD)
  Data/          RaceDayDbContext (EF Core configuration, keys, constraints)
  Dtos/          Request and response shapes
  Security/      Password hashing and session role authorisation
RaceDay.Tests/   xUnit tests (auth, events, role access, enrolments, results)
docs/            ERD, endpoint plan, SQL script
.github/workflows/dotnet.yml   CI: build + test on every push

Authentication design
Passwords are hashed with salted PBKDF2 (SHA-256); the original password is never stored.
On login, the user's id and role are saved in server-side session state (cookie-based).
A custom [SessionAuthorize] attribute protects endpoints: no session → 401, wrong role → 403.


