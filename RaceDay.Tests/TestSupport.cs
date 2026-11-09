using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

// Starts the real API in memory for tests, but swaps SQL Server for a throw-away in-memory database.
public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server registrations made in Program.cs
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(RaceDayDbContext) ||
                d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>) ||
                (d.ServiceType.IsGenericType && d.ServiceType.GenericTypeArguments.Contains(typeof(RaceDayDbContext))))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

// Small shapes used to read IDs back from JSON responses
public record EventIdResult(int EventID);
public record CategoryIdResult(int CategoryID);
public record EnrolmentItem(int RegistrationID, int EventID, int CategoryID, string ParticipantName, string BibNumber);
public record ResultItem(int ResultID, int? OverallRank, string Status, string ParticipantName);

public static class Helpers
{
    public const string Password = "Password123!";

    /// Registers a new user with a unique email and logs them in. Returns a client that holds the session cookie.
    public static async Task<HttpClient> LoggedInClient(RaceDayFactory f, string role)
    {
        var client = f.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.com";
        var isParticipant = role == "Participant";

        var reg = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Test User", email, password = Password, role, phone = "0710000000",
            dateOfBirth = isParticipant ? "1995-05-05" : null,
            gender = isParticipant ? "Male" : null
        });
        reg.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        return client;
    }

    public static object EventBody(string type = "Run") => new
    {
        name = "Test Event", description = "A test event", eventDate = "2026-12-01",
        location = "Johannesburg", distanceKm = 10, eventType = type
    };

    public static async Task<int> CreateEvent(HttpClient organiser)
    {
        var r = await organiser.PostAsJsonAsync("/api/events", EventBody());
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<EventIdResult>())!.EventID;
    }

    public static async Task<int> CreateCategory(HttpClient organiser, int eventId, string name = "10km Open")
    {
        var r = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name, distanceKm = 10, minAge = 16, gender = "Any" });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CategoryIdResult>())!.CategoryID;
    }

    public static async Task<EnrolmentItem> Enrol(HttpClient participant, int eventId, int categoryId)
    {
        var r = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new { categoryId });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<EnrolmentItem>())!;
    }
}
