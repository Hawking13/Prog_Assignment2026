# Prog_Assignment2026
All files are here
The ERD and Sql Code are also in the PDF
YouTube Link: https://youtu.be/DCly89WCv1o

The POE stuff is everything that is in branch 3 
YouTube Link for part 3: https://youtu.be/a1MEb3GhZBs


## Part 3 – MVC front end, Azure Blob Storage

### Architecture
- **RaceDay.Api** – ASP.NET Core Web API (Part 2, extended). Owns all data access and all Azure Blob Storage access.
- **RaceDay.Web** – ASP.NET Core MVC front end. Contains **no database code and no business rules**; every operation is an HTTP call to the API. The API's session cookie is kept inside the MVC session so each user stays logged in as themselves. The MVC app never talks to Azure; image uploads are sent to the API, which stores them in Azure Blob Storage.

### Roles and navigation
| Role | Pages |
|---|---|
| **Organiser** | Dashboard (events, enrolments per event, upcoming dates), My Events (create / edit / delete, banner upload), Categories, Enrolments (set Pending / Confirmed / Cancelled), Results (capture finish time and position, publish). |
| **Participant** | Home (upcoming events with filters), Event details and categories, Enrol, My Enrolments (with status), My Results (event, date, category, time, position out of finishers), My Profile (update details, upload profile picture). |
| **Not logged in** | Browse events and event details, Log in, Register. All other pages redirect to the login page. |

Enrolment statuses are colour-coded: green = Confirmed, amber = Pending, red = Cancelled.

### Azure Blob Storage setup
1. Create a Storage account and a container named `raceday-images` (access level: Blob).
2. Store the connection string as a **user secret** for the API (never commit it):
   ```
   dotnet user-secrets set "AzureStorage:ConnectionString" "<DefaultEndpointsProtocol=https;AccountName=part3raceday;AccountKey=hyTLhFHuyar1tTs6DxomyAkst4sdIBUY5JpoFRVlrl/2/Hdc6NsdYfMZ+tKloFGDncVdSntBfNjT+AStAY5nAw==;EndpointSuffix=core.windows.net>" --project RaceDay.Api
   dotnet user-secrets set "AzureStorage:ContainerName" "raceday-images" --project RaceDay.Api
   ```
3. Upload endpoints (API only): `POST /api/events/{id}/banner` (Organiser, event owner) and `POST /api/users/me/picture` (Participant). JPG, PNG, WEBP or GIF, max 5 MB.

### Running locally (no Docker)
1. Set the SQL Server connection string (`appsettings.json` or user secrets) and run `Update-Database` / `dotnet ef database update --project RaceDay.Api`.
2. Start `RaceDay.Api` and `RaceDay.Web` (Visual Studio: multiple startup projects). Check `Api:BaseUrl` in `RaceDay.Web/appsettings.json`.

### Changes made to the Part 1 / Part 2 design
| Change | Reason |
|---|---|
| `Event.BannerUrl` and `Participant.ProfilePictureUrl` columns | Store Azure Blob image URLs. |
| `Registration.Status` (Pending / Confirmed / Cancelled) | Required enrolment status; new enrolments start as Pending. |
| `Result.IsPublished` | Organisers publish results; Participants only see published results. |
| `PUT /api/enrolments/{id}/status` | Organiser confirms or cancels enrolments. |
| `GET /api/users/me/events` | Organiser dashboard: own events with enrolment counts. |
| `POST /api/events/{id}/banner`, `POST /api/users/me/picture` | Image uploads via the API to Azure Blob Storage. |
| Result responses include event date and finisher count | Display "47th out of 312 finishers". |
| `search` filter on `GET /api/events` | Home page filtering. |
| Earlier Part 2 changes (PasswordHash, logout, 401/409 responses) | See the Part 2 section. |


