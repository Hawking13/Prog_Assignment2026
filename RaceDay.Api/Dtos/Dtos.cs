using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

// DTO = "Data Transfer Object": the exact JSON shape the API accepts or returns.

public class RegisterDto
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be 'Organiser' or 'Participant'.")]
    public string Role { get; set; } = "";
    [MaxLength(20)] public string? Phone { get; set; }
    /// Required when Role is Participant.
    public DateOnly? DateOfBirth { get; set; }
    /// Required when Role is Participant: Male, Female or Other.
    [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female or Other.")]
    public string? Gender { get; set; }
}

public class LoginDto
{
    [Required] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class ProfileUpdateDto
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [MaxLength(20)] public string? Phone { get; set; }
}

public class EventDto
{
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    /// Format: yyyy-MM-dd
    public DateOnly EventDate { get; set; }
    [Required, MaxLength(150)] public string Location { get; set; } = "";
    [Range(0.01, 1000)] public decimal DistanceKm { get; set; }
    /// Run, Walk or Cycle.
    [Required, RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = "";
}

public class CategoryDto
{
    [Required, MaxLength(50)] public string Name { get; set; } = "";
    [Range(0.01, 1000)] public decimal DistanceKm { get; set; }
    [Range(0, 120)] public int? MinAge { get; set; }
    [Range(0, 120)] public int? MaxAge { get; set; }
    /// Male, Female or Any.
    [RegularExpression("^(Male|Female|Any)$", ErrorMessage = "Gender must be Male, Female or Any.")]
    public string Gender { get; set; } = "Any";
}

public class EnrolDto
{
    /// The category the participant is entering.
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
}

public class EnrolmentStatusDto
{
    /// Pending, Confirmed or Cancelled.
    [Required, RegularExpression("^(Pending|Confirmed|Cancelled)$", ErrorMessage = "Status must be Pending, Confirmed or Cancelled.")]
    public string Status { get; set; } = "";
}

public class ResultDto
{
    /// Format hh:mm:ss, e.g. 00:52:34
    public TimeSpan? FinishTime { get; set; }
    [Range(1, 100000)] public int? OverallRank { get; set; }
    /// Registered, Finished, DNF or DSQ.
    [RegularExpression("^(Registered|Finished|DNF|DSQ)$", ErrorMessage = "Status must be Registered, Finished, DNF or DSQ.")]
    public string Status { get; set; } = "Finished";
    /// true = visible to the Participant (published). false = saved but hidden.
    public bool IsPublished { get; set; } = true;
}

// ---------- Responses ----------
public record UserResponse(int Id, string Name, string Email, string Role, string? Phone, DateOnly? DateOfBirth,
    string? Gender, string? ProfilePictureUrl = null);

public record EventResponse(int EventID, int OrganiserID, string Name, string? Description, DateOnly EventDate,
    string Location, decimal DistanceKm, string EventType, string? BannerUrl = null);

public record OrganiserEventResponse(int EventID, string Name, string? Description, DateOnly EventDate, string Location,
    decimal DistanceKm, string EventType, string? BannerUrl, int EnrolmentCount, int ConfirmedCount);

public record CategoryResponse(int CategoryID, int EventID, string Name, decimal DistanceKm, int? MinAge, int? MaxAge, string Gender);

public record EnrolmentResponse(int RegistrationID, int EventID, string EventName, DateOnly EventDate, int CategoryID,
    string CategoryName, int ParticipantID, string ParticipantName, string BibNumber, DateTime RegistrationDate, string Status);

public record ResultResponse(int ResultID, int RegistrationID, int EventID, string EventName, DateOnly EventDate,
    string ParticipantName, string BibNumber, string CategoryName, TimeSpan? FinishTime, int? OverallRank,
    int FinishersCount, string Status, bool IsPublished);

public record ImageUploadResponse(string Url);
