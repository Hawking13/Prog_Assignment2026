using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
[Route("Organiser/Events")]
public class OrganiserEventsController : BaseController
{
    public OrganiserEventsController(ApiClient api) : base(api) { }

    // Loads an event and makes sure it belongs to the logged-in Organiser (the API enforces this too)
    private async Task<(EventDto? Event, IActionResult? Redirect)> LoadOwnEvent(int id)
    {
        var ev = await Api.GetAsync<EventDto>($"api/events/{id}");
        if (!ev.Ok || ev.Data == null)
        {
            Fail(ev.Status == 404 ? "That event could not be found." : ev.Error ?? "The event could not be loaded.");
            return (null, RedirectToAction(nameof(Index)));
        }
        if (ev.Data.OrganiserID != CurrentUserId)
        {
            Fail("You can only manage your own events.");
            return (null, RedirectToAction(nameof(Index)));
        }
        return (ev.Data, null);
    }

    // ---------------- Events ----------------

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var res = await Api.GetAsync<List<OrganiserEventDto>>("api/users/me/events");
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (!res.Ok) Fail(res.Error ?? "Your events could not be loaded.");
        return View(res.Data ?? new List<OrganiserEventDto>());
    }

    [HttpGet("Create")]
    public IActionResult Create() => View(new EventForm { EventDate = DateTime.Today.AddDays(30) });

    [HttpPost("Create")]
    public async Task<IActionResult> Create(EventForm form)
    {
        if (!ModelState.IsValid) return View(form);

        var res = await Api.SendAsync<EventDto>(HttpMethod.Post, "api/events", ToBody(form));
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (!res.Ok || res.Data == null)
        {
            ModelState.AddModelError("", res.Error ?? "The event could not be created.");
            return View(form);
        }

        if (form.Banner is { Length: > 0 })
        {
            var up = await Api.UploadAsync<ImageUploadDto>($"api/events/{res.Data.EventID}/banner", form.Banner);
            if (!up.Ok)
            {
                Fail($"The event was created, but the banner could not be uploaded: {up.Error}");
                return RedirectToAction(nameof(Index));
            }
        }

        Success("Event created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/Edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var (ev, redirect) = await LoadOwnEvent(id);
        if (redirect != null) return redirect;
        ViewData["EventId"] = id;
        return View(new EventForm
        {
            Name = ev!.Name, Description = ev.Description, EventDate = ev.EventDate.ToDateTime(TimeOnly.MinValue),
            Location = ev.Location, DistanceKm = ev.DistanceKm, EventType = ev.EventType, ExistingBannerUrl = ev.BannerUrl
        });
    }

    [HttpPost("{id:int}/Edit")]
    public async Task<IActionResult> Edit(int id, EventForm form)
    {
        ViewData["EventId"] = id;
        if (!ModelState.IsValid) return View(form);

        var res = await Api.SendAsync<EventDto>(HttpMethod.Put, $"api/events/{id}", ToBody(form));
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (!res.Ok)
        {
            ModelState.AddModelError("", res.Error ?? "The event could not be updated.");
            return View(form);
        }

        if (form.Banner is { Length: > 0 })
        {
            var up = await Api.UploadAsync<ImageUploadDto>($"api/events/{id}/banner", form.Banner);
            if (!up.Ok)
            {
                Fail($"The event was updated, but the banner could not be uploaded: {up.Error}");
                return RedirectToAction(nameof(Index));
            }
        }

        Success("Event updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var res = await Api.SendAsync<object>(HttpMethod.Delete, $"api/events/{id}");
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (res.Ok) Success("Event deleted.");
        else Fail(res.Error ?? "The event could not be deleted.");
        return RedirectToAction(nameof(Index));
    }

    private static object ToBody(EventForm f) => new
    {
        name = f.Name, description = f.Description,
        eventDate = DateOnly.FromDateTime(f.EventDate ?? DateTime.Today),
        location = f.Location, distanceKm = f.DistanceKm, eventType = f.EventType
    };

    // ---------------- Categories ----------------

    [HttpGet("{id:int}/Categories")]
    public async Task<IActionResult> Categories(int id)
    {
        var (ev, redirect) = await LoadOwnEvent(id);
        if (redirect != null) return redirect;
        var cats = await Api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
        return View(new CategoriesVm { Event = ev!, Categories = cats.Data ?? new() });
    }

    [HttpPost("{id:int}/Categories")]
    public async Task<IActionResult> AddCategory(int id, CategoryForm form)
    {
        if (form.MinAge > form.MaxAge) ModelState.AddModelError(nameof(form.MinAge), "Minimum age cannot be greater than maximum age.");

        if (!ModelState.IsValid)
        {
            var (ev, redirect) = await LoadOwnEvent(id);
            if (redirect != null) return redirect;
            var cats = await Api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
            return View("Categories", new CategoriesVm { Event = ev!, Categories = cats.Data ?? new(), Form = form });
        }

        var res = await Api.SendAsync<CategoryDto>(HttpMethod.Post, $"api/events/{id}/categories",
            new { name = form.Name, distanceKm = form.DistanceKm, minAge = form.MinAge, maxAge = form.MaxAge, gender = form.Gender });
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (res.Ok) Success("Category added.");
        else Fail(res.Error ?? "The category could not be added.");
        return RedirectToAction(nameof(Categories), new { id });
    }

    [HttpPost("{id:int}/Categories/{categoryId:int}/Delete")]
    public async Task<IActionResult> DeleteCategory(int id, int categoryId)
    {
        var res = await Api.SendAsync<object>(HttpMethod.Delete, $"api/categories/{categoryId}");
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (res.Ok) Success("Category deleted.");
        else Fail(res.Error ?? "The category could not be deleted.");
        return RedirectToAction(nameof(Categories), new { id });
    }

    // ---------------- Enrolments ----------------

    [HttpGet("{id:int}/Enrolments")]
    public async Task<IActionResult> Enrolments(int id)
    {
        var (ev, redirect) = await LoadOwnEvent(id);
        if (redirect != null) return redirect;
        var res = await Api.GetAsync<List<EnrolmentDto>>($"api/events/{id}/enrolments");
        if (!res.Ok) Fail(res.Error ?? "The enrolments could not be loaded.");
        return View(new EnrolmentsPageVm { Event = ev!, Enrolments = res.Data ?? new() });
    }

    [HttpPost("{id:int}/Enrolments/{regId:int}/Status")]
    public async Task<IActionResult> SetStatus(int id, int regId, string status)
    {
        var res = await Api.SendAsync<EnrolmentDto>(HttpMethod.Put, $"api/enrolments/{regId}/status", new { status });
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (res.Ok) Success($"Enrolment marked as {status}.");
        else Fail(res.Error ?? "The status could not be changed.");
        return RedirectToAction(nameof(Enrolments), new { id });
    }

    // ---------------- Results ----------------

    [HttpGet("{id:int}/Results")]
    public async Task<IActionResult> Results(int id)
    {
        var (ev, redirect) = await LoadOwnEvent(id);
        if (redirect != null) return redirect;

        var enrolments = await Api.GetAsync<List<EnrolmentDto>>($"api/events/{id}/enrolments");
        var results = await Api.GetAsync<List<ResultDto>>($"api/events/{id}/results");
        if (!enrolments.Ok) Fail(enrolments.Error ?? "The enrolments could not be loaded.");

        var rows = (enrolments.Data ?? new()).Where(e => e.Status != "Cancelled").Select(e =>
        {
            var r = results.Data?.FirstOrDefault(x => x.RegistrationID == e.RegistrationID);
            return new ResultRow
            {
                Enrolment = e, Result = r,
                Form = r == null ? new ResultForm() : new ResultForm
                {
                    ResultId = r.ResultID, FinishTimeText = r.FinishTime.HasValue ? r.FinishTime.Value.ToString(@"hh\:mm\:ss") : null,
                    OverallRank = r.OverallRank, Status = r.Status, IsPublished = r.IsPublished
                }
            };
        }).ToList();

        return View(new ResultsPageVm { Event = ev!, Rows = rows });
    }

    [HttpPost("{id:int}/Results/{regId:int}")]
    public async Task<IActionResult> SaveResult(int id, int regId, ResultForm form)
    {
        if (!ModelState.IsValid)
        {
            var first = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            Fail(first ?? "Please check the result details.");
            return RedirectToAction(nameof(Results), new { id });
        }

        string? time = null;
        if (!string.IsNullOrWhiteSpace(form.FinishTimeText))
        {
            if (!TimeSpan.TryParseExact(form.FinishTimeText, @"h\:mm\:ss", CultureInfo.InvariantCulture, out var ts))
            {
                Fail("Finish time must look like 00:52:34 (hh:mm:ss).");
                return RedirectToAction(nameof(Results), new { id });
            }
            time = ts.ToString("c");
        }

        var body = new { finishTime = time, overallRank = form.OverallRank, status = form.Status, isPublished = form.IsPublished };
        var res = form.ResultId == null
            ? await Api.SendAsync<ResultDto>(HttpMethod.Post, $"api/enrolments/{regId}/result", body)
            : await Api.SendAsync<ResultDto>(HttpMethod.Put, $"api/results/{form.ResultId}", body);

        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (res.Ok) Success(form.IsPublished ? "Result saved and published." : "Result saved (hidden from the participant).");
        else Fail(res.Error ?? "The result could not be saved.");
        return RedirectToAction(nameof(Results), new { id });
    }
}
