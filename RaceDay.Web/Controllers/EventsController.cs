using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController : BaseController
{
    public EventsController(ApiClient api) : base(api) { }

    // Public: full event info and categories
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var ev = await Api.GetAsync<EventDto>($"api/events/{id}");
        if (!ev.Ok || ev.Data == null)
        {
            Fail(ev.Status == 404 ? "That event could not be found." : ev.Error ?? "The event could not be loaded.");
            return RedirectToAction("Index", "Home");
        }

        var cats = await Api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
        var vm = new EventDetailsVm { Event = ev.Data, Categories = cats.Data ?? new() };

        if (CurrentRole == "Participant")
        {
            var mine = await Api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");
            if (IsExpired(mine)) return RedirectToAction("Login", "Account");
            vm.MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventID == id);
        }
        return View(vm);
    }

    // Participants only: enter an event by choosing a category (recorded immediately by the API)
    [HttpPost, RoleAuthorize("Participant")]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (categoryId <= 0)
        {
            Fail("Please choose a category before enrolling.");
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var res = await Api.SendAsync<EnrolmentDto>(HttpMethod.Post, $"api/events/{eventId}/enrol", new { categoryId });
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (!res.Ok)
        {
            Fail(res.Error ?? "We could not enrol you. Please try again.");
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        Success("You are enrolled! Your enrolment is pending until the organiser confirms it.");
        return RedirectToAction("Enrolments", "Participant");
    }
}
