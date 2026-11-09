using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
public class OrganiserController : BaseController
{
    public OrganiserController(ApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var res = await Api.GetAsync<List<OrganiserEventDto>>("api/users/me/events");
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (!res.Ok) Fail(res.Error ?? "Your events could not be loaded.");
        return View(new DashboardVm { Events = res.Data ?? new() });
    }
}
