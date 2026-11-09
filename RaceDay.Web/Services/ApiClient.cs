using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public bool Ok { get; init; }
    public int Status { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public bool Unauthorized => Status == 401;
}

/// Talks to the Part 2 API. Keeps the API's session cookie inside the MVC session
/// so each MVC user stays logged in to the API as themselves.
public class ApiClient
{
    public const string CookieKey = "ApiCookie";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;

    public ApiClient(IHttpClientFactory factory, IHttpContextAccessor ctx)
    {
        _http = factory.CreateClient("RaceDayApi");
        _ctx = ctx;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url, null);

    public async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        using var req = new HttpRequestMessage(method, url);
        if (body != null) req.Content = JsonContent.Create(body, body.GetType(), null, Json);
        return await ExecuteAsync<T>(req);
    }

    public async Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        return await ExecuteAsync<T>(req);
    }

    private async Task<ApiResult<T>> ExecuteAsync<T>(HttpRequestMessage req)
    {
        var session = _ctx.HttpContext?.Session;
        var cookie = session?.GetString(CookieKey);
        if (!string.IsNullOrEmpty(cookie)) req.Headers.TryAddWithoutValidation("Cookie", cookie);

        try
        {
            using var resp = await _http.SendAsync(req);
            CaptureCookie(resp, session);
            var text = await resp.Content.ReadAsStringAsync();
            var status = (int)resp.StatusCode;

            if (resp.IsSuccessStatusCode)
            {
                T? data = default;
                if (!string.IsNullOrWhiteSpace(text) && typeof(T) != typeof(object))
                    data = JsonSerializer.Deserialize<T>(text, Json);
                return new ApiResult<T> { Ok = true, Status = status, Data = data };
            }
            return new ApiResult<T> { Ok = false, Status = status, Error = ParseError(text, status) };
        }
        catch (HttpRequestException)
        {
            return Fail<T>(503, "The RaceDay service is not reachable right now. Please try again shortly.");
        }
        catch (TaskCanceledException)
        {
            return Fail<T>(504, "The RaceDay service took too long to respond. Please try again.");
        }
        catch (JsonException)
        {
            return Fail<T>(500, "The RaceDay service sent a response we could not understand.");
        }
    }

    private static ApiResult<T> Fail<T>(int status, string message) =>
        new() { Ok = false, Status = status, Error = message };

    private static void CaptureCookie(HttpResponseMessage resp, ISession? session)
    {
        if (session == null || !resp.Headers.TryGetValues("Set-Cookie", out var values)) return;
        foreach (var v in values)
            if (v.StartsWith(".RaceDay.Session=", StringComparison.Ordinal))
                session.SetString(CookieKey, v.Split(';')[0]);
    }

    // Turns the API's error JSON into one friendly sentence (never raw JSON or stack traces)
    private static string ParseError(string text, int status)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                        return m.GetString()!;
                    if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Object)
                    {
                        var msgs = new List<string>();
                        foreach (var prop in errs.EnumerateObject())
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                                foreach (var e in prop.Value.EnumerateArray())
                                    if (e.ValueKind == JsonValueKind.String) msgs.Add(e.GetString()!);
                        if (msgs.Count > 0) return string.Join(" ", msgs.Distinct());
                    }
                }
            }
        }
        catch (JsonException) { }

        return status switch
        {
            400 => "Some of the information entered is not valid.",
            401 => "Please log in to continue.",
            403 => "You do not have permission to do that.",
            404 => "That item could not be found.",
            409 => "That conflicts with existing information.",
            _ => "Something went wrong. Please try again."
        };
    }
}
