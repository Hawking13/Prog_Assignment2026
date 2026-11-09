namespace RaceDay.Web.Models;

// Shapes of the JSON the Part 2 API returns.

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class EventDto
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = "";
    public string? BannerUrl { get; set; }
}

public class OrganiserEventDto
{
    public int EventID { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = "";
    public string? BannerUrl { get; set; }
    public int EnrolmentCount { get; set; }
    public int ConfirmedCount { get; set; }
}

public class CategoryDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public string Gender { get; set; } = "Any";
}

public class EnrolmentDto
{
    public int RegistrationID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = "";
    public string BibNumber { get; set; } = "";
    public DateTime RegistrationDate { get; set; }
    public string Status { get; set; } = "Pending";
}

public class ResultDto
{
    public int ResultID { get; set; }
    public int RegistrationID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public string ParticipantName { get; set; } = "";
    public string BibNumber { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public TimeSpan? FinishTime { get; set; }
    public int? OverallRank { get; set; }
    public int FinishersCount { get; set; }
    public string Status { get; set; } = "";
    public bool IsPublished { get; set; }
}

public class ImageUploadDto
{
    public string Url { get; set; } = "";
}
