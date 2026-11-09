using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class BaseController : Controller
{
    protected readonly ApiClient Api;
    protected BaseController(ApiClient api) => Api = api;

    protected string? CurrentRole => HttpContext.Session.GetString(SessionKeys.Role);
    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    protected void Success(string message) => TempData["Success"] = message;
    protected void Fail(string message) => TempData["Error"] = message;

    /// If the API says "not logged in" (e.g. its session timed out), end our session too.
    protected bool IsExpired<T>(ApiResult<T> result)
    {
        if (!result.Unauthorized) return false;
        HttpContext.Session.Clear();
        Fail("Your session has expired. Please log in again.");
        return true;
    }
}
