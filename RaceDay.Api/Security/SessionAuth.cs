using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Security;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

// Reads/writes the logged-in user's id and role in the server-side session.
public static class SessionHelpers
{
    public static void SetUser(this ISession s, int id, string role)
    {
        s.SetInt32("UserId", id);
        s.SetString("Role", role);
    }
    public static int? GetUserId(this ISession s) => s.GetInt32("UserId");
    public static string? GetRole(this ISession s) => s.GetString("Role");
}

/// Put [SessionAuthorize] on an endpoint to require login, or
/// [SessionAuthorize(Roles.Organiser)] to require a specific role.
/// Not logged in -> 401 Unauthorized. Wrong role -> 403 Forbidden.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;
    public SessionAuthorizeAttribute(params string[] roles) => _roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetUserId();
        var role = session.GetRole();

        if (userId is null || role is null)
        {
            context.Result = new ObjectResult(new { message = "You must be logged in." }) { StatusCode = 401 };
            return;
        }
        if (_roles.Length > 0 && !_roles.Contains(role))
        {
            context.Result = new ObjectResult(new { message = $"This action requires the role: {string.Join(" or ", _roles)}." }) { StatusCode = 403 };
        }
    }
}
