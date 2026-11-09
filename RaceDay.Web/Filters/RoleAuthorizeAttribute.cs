using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Helpers;

namespace RaceDay.Web.Filters;

/// [RoleAuthorize("Organiser")] on a controller or action: not logged in -> Login page,
/// wrong role -> Access Denied page.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _role;
    public RoleAuthorizeAttribute(string role) => _role = role;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var role = context.HttpContext.Session.GetString(SessionKeys.Role);
        if (role == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        if (role != _role)
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
