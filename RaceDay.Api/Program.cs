using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceDay.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Database (connection string lives in appsettings.json)
builder.Services.AddDbContext<RaceDayDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb")));

// 2. Controllers
builder.Services.AddControllers();

// 3. Sessions: the server remembers who is logged in (and their role) using a cookie
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.Name = ".RaceDay.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// 4. Swagger (the test page at /swagger), including the /// comments from the controllers
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "Event management API for South African road running, walking and cycling events. " +
                      "Log in via POST /api/auth/login first; Swagger then sends the session cookie automatically."
    });
    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseSession();          // must come before MapControllers
app.MapControllers();
app.Run();

// Lets the test project start the whole API in memory
public partial class Program { }
