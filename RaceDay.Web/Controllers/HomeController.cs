using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : BaseController
{
    public HomeController(ApiClient api) : base(api) { }

    // Home page = upcoming events with filters (public). Organisers go to their dashboard.
    public async Task<IActionResult> Index(string? search, string? type, string? location, [FromQuery(Name = "from")] DateTime? startDate)
    {
        if (CurrentRole == "Organiser") return RedirectToAction("Dashboard", "Organiser");

        var from = startDate ?? DateTime.Today;
        var url = $"api/events?from={from:yyyy-MM-dd}";
        if (!string.IsNullOrWhiteSpace(search)) url += "&search=" + Uri.EscapeDataString(search);
        if (!string.IsNullOrWhiteSpace(type)) url += "&type=" + Uri.EscapeDataString(type);
        if (!string.IsNullOrWhiteSpace(location)) url += "&location=" + Uri.EscapeDataString(location);

        var res = await Api.GetAsync<List<EventDto>>(url);
        return View(new HomeVm
        {
            Events = res.Data ?? new(), Search = search, Type = type, Location = location, From = from,
            ErrorMessage = res.Ok ? null : res.Error
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code)
    {
        var c = code ?? 500;
        var vm = c switch
        {
            404 => new ErrorVm { Code = 404, Title = "Page not found", Message = "We could not find the page you were looking for." },
            403 => new ErrorVm { Code = 403, Title = "Access denied", Message = "You do not have permission to view that page." },
            400 => new ErrorVm { Code = 400, Title = "Bad request", Message = "That request could not be processed." },
            _ => new ErrorVm { Code = c, Title = "Something went wrong", Message = "An unexpected problem occurred. Please try again in a moment." }
        };
        Response.StatusCode = c;
        return View(vm);
    }
}
