using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

// Each class below = one table from the ERD. Each property = one column.

public class Organiser
{
    [Key] public int OrganiserID { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, MaxLength(150)] public string Email { get; set; } = "";
    [MaxLength(20)] public string? Phone { get; set; }
    // Added in Part 2 so users can log in. Only the HASH is stored, never the real password.
    [Required, MaxLength(200)] public string PasswordHash { get; set; } = "";
    public List<Event> Events { get; set; } = new();
}

public class Participant
{
    [Key] public int ParticipantID { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, MaxLength(150)] public string Email { get; set; } = "";
    [MaxLength(20)] public string? Phone { get; set; }
    public DateOnly DateOfBirth { get; set; }
    [Required, MaxLength(10)] public string Gender { get; set; } = "";
    [Required, MaxLength(200)] public string PasswordHash { get; set; } = "";
    [MaxLength(500)] public string? ProfilePictureUrl { get; set; }             // Azure Blob URL (Part 3)
    public List<Registration> Registrations { get; set; } = new();
}

public class Event
{
    [Key] public int EventID { get; set; }
    public int OrganiserID { get; set; }                 // foreign key
    public Organiser Organiser { get; set; } = null!;    // navigation property
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    [Required, MaxLength(150)] public string Location { get; set; } = "";
    public decimal DistanceKm { get; set; }
    [Required, MaxLength(10)] public string EventType { get; set; } = "Run";   // Run | Walk | Cycle
    [MaxLength(500)] public string? BannerUrl { get; set; }                     // Azure Blob URL (Part 3)
    public List<Category> Categories { get; set; } = new();
    public List<Route> Routes { get; set; } = new();
}

public class Category
{
    [Key] public int CategoryID { get; set; }
    public int EventID { get; set; }
    public Event Event { get; set; } = null!;
    [Required, MaxLength(50)] public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    [Required, MaxLength(10)] public string Gender { get; set; } = "Any";
    public List<Registration> Registrations { get; set; } = new();
}

public class Route
{
    [Key] public int RouteID { get; set; }
    public int EventID { get; set; }
    public Event Event { get; set; } = null!;
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public string? ElevationProfile { get; set; }
    [MaxLength(255)] public string? MapURL { get; set; }
}

// Registration = an "enrolment": links Participant + Category (and so the Event)
public class Registration
{
    [Key] public int RegistrationID { get; set; }
    public int ParticipantID { get; set; }
    public Participant Participant { get; set; } = null!;
    public int CategoryID { get; set; }
    public Category Category { get; set; } = null!;
    [Required, MaxLength(10)] public string BibNumber { get; set; } = "";
    public DateTime RegistrationDate { get; set; } = DateTime.Now;
    [Required, MaxLength(20)] public string Status { get; set; } = "Pending";   // Pending | Confirmed | Cancelled
    public Result? Result { get; set; }
}

public class Result
{
    [Key] public int ResultID { get; set; }
    public int RegistrationID { get; set; }
    public Registration Registration { get; set; } = null!;
    public TimeSpan? FinishTime { get; set; }
    public int? OverallRank { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Registered";
    public bool IsPublished { get; set; } = true;   // only published results are visible to Participants
}
