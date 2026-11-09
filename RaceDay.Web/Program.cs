using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC. Every POST form must carry an anti-forgery token (protects against forged requests).
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// Session: remembers who is logged in (and their role) across pages.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.Name = ".RaceDay.Web.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// The ONLY way this app gets data: HTTP calls to the Part 2 API.
builder.Services.AddHttpContextAccessor();
var apiBase = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7061/";
if (!apiBase.EndsWith('/')) apiBase += "/";
builder.Services.AddHttpClient("RaceDayApi", c =>
{
    c.BaseAddress = new Uri(apiBase);
    c.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    // UseCookies=false: we attach the API's session cookie ourselves, per logged-in user.
    var handler = new HttpClientHandler { UseCookies = false };
    if (builder.Configuration.GetValue<bool>("Api:AllowUntrustedCertificate"))
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    return handler;
});
builder.Services.AddScoped<ApiClient>();

var app = builder.Build();

// Friendly error pages instead of raw exceptions
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();   // must come before MapControllerRoute
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
