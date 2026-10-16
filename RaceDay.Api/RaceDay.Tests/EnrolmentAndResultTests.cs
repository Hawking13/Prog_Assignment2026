using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class EnrolmentAndResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _f;
    public EnrolmentAndResultTests(RaceDayFactory f) => _f = f;

    [Fact]
    public async Task Participant_Enrol_Returns201_AndEnrolmentIsRecorded()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var p = await Helpers.LoggedInClient(_f, "Participant");
        var eventId = await Helpers.CreateEvent(org);
        var catId = await Helpers.CreateCategory(org, eventId);

        var r = await p.PostAsJsonAsync($"/api/events/{eventId}/enrol", new { categoryId = catId });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        // Participant sees it in their own list
        var mine = await p.GetFromJsonAsync<List<EnrolmentItem>>("/api/users/me/enrolments");
        Assert.Contains(mine!, e => e.EventID == eventId && e.CategoryID == catId);

        // Organiser sees it in the event's enrolments
        var forEvent = await org.GetFromJsonAsync<List<EnrolmentItem>>($"/api/events/{eventId}/enrolments");
        Assert.Single(forEvent!);
        Assert.Equal(catId, forEvent![0].CategoryID);
    }

    [Fact]
    public async Task Participant_EnrolTwiceInSameEvent_Returns409()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var p = await Helpers.LoggedInClient(_f, "Participant");
        var eventId = await Helpers.CreateEvent(org);
        var catId = await Helpers.CreateCategory(org, eventId);
        await Helpers.Enrol(p, eventId, catId);

        var again = await p.PostAsJsonAsync($"/api/events/{eventId}/enrol", new { categoryId = catId });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Participant_EnrolWithCategoryFromAnotherEvent_Returns404()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var p = await Helpers.LoggedInClient(_f, "Participant");
        var eventA = await Helpers.CreateEvent(org);
        var eventB = await Helpers.CreateEvent(org);
        var catB = await Helpers.CreateCategory(org, eventB);

        var r = await p.PostAsJsonAsync($"/api/events/{eventA}/enrol", new { categoryId = catB });
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_Enrol_Returns403()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var eventId = await Helpers.CreateEvent(org);
        var catId = await Helpers.CreateCategory(org, eventId);
        var r = await org.PostAsJsonAsync($"/api/events/{eventId}/enrol", new { categoryId = catId });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Anonymous_Enrol_Returns401()
    {
        var r = await _f.CreateClient().PostAsJsonAsync("/api/events/1/enrol", new { categoryId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewEventEnrolments_Returns403()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var p = await Helpers.LoggedInClient(_f, "Participant");
        var eventId = await Helpers.CreateEvent(org);
        var r = await p.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_ViewEventEnrolments_Returns403()
    {
        var owner = await Helpers.LoggedInClient(_f, "Organiser");
        var other = await Helpers.LoggedInClient(_f, "Organiser");
        var eventId = await Helpers.CreateEvent(owner);
        var r = await other.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Participant_Withdraw_Returns204()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var p = await Helpers.LoggedInClient(_f, "Participant");
        var eventId = await Helpers.CreateEvent(org);
        var catId = await Helpers.CreateCategory(org, eventId);
        var enrolment = await Helpers.Enrol(p, eventId, catId);

        var r = await p.DeleteAsync($"/api/enrolments/{enrolment.RegistrationID}");
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CaptureResult_Returns201_AndParticipantSeesOwnResult()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var p = await Helpers.LoggedInClient(_f, "Participant");
        var eventId = await Helpers.CreateEvent(org);
        var catId = await Helpers.CreateCategory(org, eventId);
        var enrolment = await Helpers.Enrol(p, eventId, catId);

        var r = await org.PostAsJsonAsync($"/api/enrolments/{enrolment.RegistrationID}/result",
            new { finishTime = "00:52:34", overallRank = 3, status = "Finished" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        var mine = await p.GetFromJsonAsync<List<ResultItem>>("/api/users/me/results");
        Assert.Single(mine!);
        Assert.Equal(3, mine![0].OverallRank);
    }

    [Fact]
    public async Task Participant_CaptureResult_Returns403()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var p = await Helpers.LoggedInClient(_f, "Participant");
        var eventId = await Helpers.CreateEvent(org);
        var catId = await Helpers.CreateCategory(org, eventId);
        var enrolment = await Helpers.Enrol(p, eventId, catId);

        var r = await p.PostAsJsonAsync($"/api/enrolments/{enrolment.RegistrationID}/result",
            new { finishTime = "00:40:00", overallRank = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CaptureResultTwice_Returns409()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var p = await Helpers.LoggedInClient(_f, "Participant");
        var eventId = await Helpers.CreateEvent(org);
        var catId = await Helpers.CreateCategory(org, eventId);
        var enrolment = await Helpers.Enrol(p, eventId, catId);
        var body = new { finishTime = "01:00:00", overallRank = 5, status = "Finished" };

        await org.PostAsJsonAsync($"/api/enrolments/{enrolment.RegistrationID}/result", body);
        var again = await org.PostAsJsonAsync($"/api/enrolments/{enrolment.RegistrationID}/result", body);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewParticipantResults_Returns403()
    {
        var org = await Helpers.LoggedInClient(_f, "Organiser");
        var r = await org.GetAsync("/api/users/me/results");
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }
}
