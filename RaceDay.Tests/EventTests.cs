using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _f;
    public EventTests(RaceDayFactory f) => _f = f;

    [Fact]
    public async Task GetEvents_WithoutLogin_Returns200()
    {
        var r = await _f.CreateClient().GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_Returns201()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var r = await org.PostAsJsonAsync("/api/events", Helpers.EventBody());
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_InvalidType_Returns400()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var r = await org.PostAsJsonAsync("/api/events", Helpers.EventBody("Swim"));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Participant_CreateEvent_Returns403()
    {
        var p = await Helpers.LoggedInClient(_f, "Participant");
        var r = await p.PostAsJsonAsync("/api/events", Helpers.EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CreateEvent_Returns401()
    {
        var r = await _f.CreateClient().PostAsJsonAsync("/api/events", Helpers.EventBody());
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdateOwnEvent_Returns200()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var id = await Helpers.CreateEvent(org);
        var r = await org.PutAsJsonAsync($"/api/events/{id}", Helpers.EventBody("Walk"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdateAnotherOrganisersEvent_Returns403()
    {
        var owner = await Helpers.LoggedInClient(_f, "Organiser");
        var other = await Helpers.LoggedInClient(_f, "Organiser");
        var id = await Helpers.CreateEvent(owner);
        var r = await other.PutAsJsonAsync($"/api/events/{id}", Helpers.EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Participant_UpdateEvent_Returns403()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var p = await Helpers.LoggedInClient(_f, "Participant");
        var id = await Helpers.CreateEvent(org);
        var r = await p.PutAsJsonAsync($"/api/events/{id}", Helpers.EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentEvent_Returns404()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var r = await org.PutAsJsonAsync("/api/events/999999", Helpers.EventBody());
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_DeleteOwnEvent_Returns204_ThenGetReturns404()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var id = await Helpers.CreateEvent(org);
        Assert.Equal(HttpStatusCode.NoContent, (await org.DeleteAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await org.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Participant_DeleteEvent_Returns403()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var p = await Helpers.LoggedInClient(_f, "Participant");
        var id = await Helpers.CreateEvent(org);
        Assert.Equal(HttpStatusCode.Forbidden, (await p.DeleteAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateCategory_Returns201()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var id = await Helpers.CreateEvent(org);
        var r = await org.PostAsJsonAsync($"/api/events/{id}/categories", new { name = "Senior", distanceKm = 10, minAge = 50, gender = "Any" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Participant_CreateCategory_Returns403()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var p = await Helpers.LoggedInClient(_f, "Participant");
        var id = await Helpers.CreateEvent(org);
        var r = await p.PostAsJsonAsync($"/api/events/{id}/categories", new { name = "Senior", distanceKm = 10, gender = "Any" });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task GetCategories_WithoutLogin_Returns200()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var id = await Helpers.CreateEvent(org);
        await Helpers.CreateCategory(org, id);
        var r = await _f.CreateClient().GetAsync($"/api/events/{id}/categories");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }
}
