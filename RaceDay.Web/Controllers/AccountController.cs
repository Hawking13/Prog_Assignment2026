using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : BaseController
{
    public AccountController(ApiClient api) : base(api) { }

    private IActionResult HomeFor(string role) =>
        role == "Organiser" ? RedirectToAction("Dashboard", "Organiser") : RedirectToAction("Index", "Home");

    [HttpGet]
    public IActionResult Login()
    {
        if (CurrentRole != null) return HomeFor(CurrentRole);
        return View(new LoginVm());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVm vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(vm);

        var res = await Api.SendAsync<UserDto>(HttpMethod.Post, "api/auth/login", new { email = vm.Email, password = vm.Password });
        if (!res.Ok || res.Data == null)
        {
            ModelState.AddModelError("", res.Status == 401 ? "Incorrect email or password." : res.Error ?? "We could not log you in. Please try again.");
            return View(vm);
        }

        var user = res.Data;
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.Id);
        HttpContext.Session.SetString(SessionKeys.Name, user.Name);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return HomeFor(user.Role);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (CurrentRole != null) return HomeFor(CurrentRole);
        return View(new RegisterVm());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (vm.Role == "Participant")
        {
            if (vm.DateOfBirth == null) ModelState.AddModelError(nameof(vm.DateOfBirth), "Participants must enter their date of birth.");
            if (string.IsNullOrWhiteSpace(vm.Gender)) ModelState.AddModelError(nameof(vm.Gender), "Participants must choose a gender.");
            if (vm.DateOfBirth > DateTime.Today) ModelState.AddModelError(nameof(vm.DateOfBirth), "Date of birth cannot be in the future.");
        }
        if (!ModelState.IsValid) return View(vm);

        var body = new
        {
            name = vm.Name, email = vm.Email, password = vm.Password, role = vm.Role, phone = vm.Phone,
            dateOfBirth = vm.Role == "Participant" && vm.DateOfBirth.HasValue ? DateOnly.FromDateTime(vm.DateOfBirth.Value) : (DateOnly?)null,
            gender = vm.Role == "Participant" ? vm.Gender : null
        };
        var res = await Api.SendAsync<UserDto>(HttpMethod.Post, "api/auth/register", body);
        if (!res.Ok)
        {
            ModelState.AddModelError("", res.Error ?? "We could not create your account. Please try again.");
            return View(vm);
        }

        Success("Your account was created. Please log in.");
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await Api.SendAsync<object>(HttpMethod.Post, "api/auth/logout");   // result ignored on purpose
        HttpContext.Session.Clear();
        Success("You have been logged out.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
