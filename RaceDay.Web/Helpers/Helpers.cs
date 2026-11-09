using System.Globalization;

namespace RaceDay.Web.Helpers;

public static class SessionKeys
{
    public const string Role = "Role";
    public const string UserId = "UserId";
    public const string Name = "Name";
}

public static class Format
{
    /// 1 -> 1st, 2 -> 2nd, 47 -> 47th
    public static string Ordinal(int n)
    {
        var suffix = (n % 100) is >= 11 and <= 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return $"{n}{suffix}";
    }

    public static string AgeRange(int? min, int? max) => (min, max) switch
    {
        (null, null) => "All ages",
        (not null, null) => $"{min}+ years",
        (null, not null) => $"Up to {max} years",
        _ => $"{min}-{max} years"
    };

    public static string Time(TimeSpan? t) => t.HasValue ? t.Value.ToString(@"hh\:mm\:ss") : "-";

    public static string Date(DateOnly d) => d.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
}
