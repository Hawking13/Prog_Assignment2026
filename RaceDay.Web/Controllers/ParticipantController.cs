using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Participant")]
public class ParticipantController : BaseController
{
    public ParticipantController(ApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Enrolments()
    {
        var res = await Api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (!res.Ok) Fail(res.Error ?? "Your enrolments could not be loaded.");
        return View(res.Data ?? new List<EnrolmentDto>());
    }

    [HttpPost]
    public async Task<IActionResult> Withdraw(int id)
    {
        var res = await Api.SendAsync<object>(HttpMethod.Delete, $"api/enrolments/{id}");
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (res.Ok) Success("You have been withdrawn from the event.");
        else Fail(res.Error ?? "We could not withdraw you. Please try again.");
        return RedirectToAction(nameof(Enrolments));
    }

    [HttpGet]
    public async Task<IActionResult> Results()
    {
        var res = await Api.GetAsync<List<ResultDto>>("api/users/me/results");
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (!res.Ok) Fail(res.Error ?? "Your results could not be loaded.");
        return View(res.Data ?? new List<ResultDto>());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var res = await Api.GetAsync<UserDto>("api/users/me");
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (!res.Ok || res.Data == null)
        {
            Fail(res.Error ?? "Your profile could not be loaded.");
            return RedirectToAction("Index", "Home");
        }
        return View(ToVm(res.Data));
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileForm form)
    {
        var current = await Api.GetAsync<UserDto>("api/users/me");
        if (IsExpired(current)) return RedirectToAction("Login", "Account");
        var user = current.Data ?? new UserDto();

        if (!ModelState.IsValid) return View(new ProfileVm { User = user, Form = form });

        var res = await Api.SendAsync<UserDto>(HttpMethod.Put, "api/users/me", new { name = form.Name, email = form.Email, phone = form.Phone });
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (!res.Ok)
        {
            ModelState.AddModelError("", res.Error ?? "Your profile could not be updated.");
            return View(new ProfileVm { User = user, Form = form });
        }

        HttpContext.Session.SetString(Helpers.SessionKeys.Name, res.Data?.Name ?? form.Name);
        Success("Your profile was updated.");
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    public async Task<IActionResult> Picture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            Fail("Please choose an image to upload.");
            return RedirectToAction(nameof(Profile));
        }

        var res = await Api.UploadAsync<ImageUploadDto>("api/users/me/picture", picture);
        if (IsExpired(res)) return RedirectToAction("Login", "Account");
        if (res.Ok) Success("Your profile picture was updated.");
        else Fail(res.Error ?? "The picture could not be uploaded.");
        return RedirectToAction(nameof(Profile));
    }

    private static ProfileVm ToVm(UserDto u) => new()
    {
        User = u,
        Form = new ProfileForm { Name = u.Name, Email = u.Email, Phone = u.Phone }
    };
}
