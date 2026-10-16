using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _f;
    public AuthTests(RaceDayFactory f) => _f = f;

    private static string NewEmail() => $"{Guid.NewGuid():N}@test.com";

    [Fact]
    public async Task Register_Participant_Returns201()
    {
        var c = _f.CreateClient();
        var r = await c.PostAsJsonAsync("/api/auth/register", new
        { name = "Pat", email = NewEmail(), password = Helpers.Password, role = "Participant", dateOfBirth = "2000-01-01", gender = "Female" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_Returns201()
    {
        var c = _f.CreateClient();
        var r = await c.PostAsJsonAsync("/api/auth/register", new
        { name = "Org", email = NewEmail(), password = Helpers.Password, role = "Organiser" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Register_ParticipantWithoutDateOfBirth_Returns400()
    {
        var c = _f.CreateClient();
        var r = await c.PostAsJsonAsync("/api/auth/register", new
        { name = "Pat", email = NewEmail(), password = Helpers.Password, role = "Participant" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var c = _f.CreateClient();
        var r = await c.PostAsJsonAsync("/api/auth/register", new
        { name = "X", email = NewEmail(), password = Helpers.Password, role = "Admin" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var c = _f.CreateClient();
        var r = await c.PostAsJsonAsync("/api/auth/register", new
        { name = "X", email = NewEmail(), password = "short", role = "Organiser" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var c = _f.CreateClient();
        var email = NewEmail();
        var body = new { name = "Org", email, password = Helpers.Password, role = "Organiser" };
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/auth/register", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/auth/register", body)).StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        var c = _f.CreateClient();
        var email = NewEmail();
        await c.PostAsJsonAsync("/api/auth/register", new { name = "Org", email, password = Helpers.Password, role = "Organiser" });
        var r = await c.PostAsJsonAsync("/api/auth/login", new { email, password = Helpers.Password });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var c = _f.CreateClient();
        var email = NewEmail();
        await c.PostAsJsonAsync("/api/auth/register", new { name = "Org", email, password = Helpers.Password, role = "Organiser" });
        var r = await c.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var c = _f.CreateClient();
        var r = await c.PostAsJsonAsync("/api/auth/login", new { email = NewEmail(), password = Helpers.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutLogin_Returns401()
    {
        var r = await _f.CreateClient().GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Profile_AfterLogin_Returns200()
    {
        var c = await Helpers.LoggedInClient(_f, "Participant");
        var r = await c.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Profile_Update_Returns200()
    {
        var c = await Helpers.LoggedInClient(_f, "Organiser");
        var r = await c.PutAsJsonAsync("/api/users/me", new { name = "New Name", email = NewEmail(), phone = "0820000000" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Logout_ThenProfile_Returns401()
    {
        var c = await Helpers.LoggedInClient(_f, "Organiser");
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/users/me")).StatusCode);
    }
}
