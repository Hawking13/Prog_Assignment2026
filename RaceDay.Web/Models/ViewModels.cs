using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// ---------- Forms (what users type in) ----------

public class LoginVm
{
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Please enter your password.")]
    public string Password { get; set; } = "";
}

public class RegisterVm
{
    [Required(ErrorMessage = "Please enter your name."), MaxLength(100)]
    public string Name { get; set; } = "";
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "Please enter a valid email address."), MaxLength(150)]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Please choose a password."), MinLength(8, ErrorMessage = "Your password must be at least 8 characters.")]
    public string Password { get; set; } = "";
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";
    [Required(ErrorMessage = "Please choose a role."), RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Please choose a role.")]
    public string Role { get; set; } = "Participant";
    [MaxLength(20)] public string? Phone { get; set; }
    [DataType(DataType.Date)] public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
}

public class EventForm
{
    [Required(ErrorMessage = "Please enter the event name."), MaxLength(150)]
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? EventDate { get; set; }
    [Required(ErrorMessage = "Please enter the location."), MaxLength(150)]
    public string Location { get; set; } = "";
    [Range(0.01, 1000, ErrorMessage = "Distance must be between 0.01 and 1000 km.")]
    public decimal DistanceKm { get; set; }
    [Required(ErrorMessage = "Please choose an event type.")]
    public string EventType { get; set; } = "Run";
    public IFormFile? Banner { get; set; }
    public string? ExistingBannerUrl { get; set; }
}

public class CategoryForm
{
    [Required(ErrorMessage = "Please enter a category name."), MaxLength(50)]
    public string Name { get; set; } = "";
    [Range(0.01, 1000, ErrorMessage = "Distance must be between 0.01 and 1000 km.")]
    public decimal DistanceKm { get; set; }
    [Range(0, 120, ErrorMessage = "Minimum age must be between 0 and 120.")] public int? MinAge { get; set; }
    [Range(0, 120, ErrorMessage = "Maximum age must be between 0 and 120.")] public int? MaxAge { get; set; }
    public string Gender { get; set; } = "Any";
}

public class ProfileForm
{
    [Required(ErrorMessage = "Please enter your name."), MaxLength(100)]
    public string Name { get; set; } = "";
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "Please enter a valid email address."), MaxLength(150)]
    public string Email { get; set; } = "";
    [MaxLength(20)] public string? Phone { get; set; }
}

public class ResultForm
{
    public int? ResultId { get; set; }
    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Finish time must look like 00:52:34 (hh:mm:ss).")]
    public string? FinishTimeText { get; set; }
    [Range(1, 100000, ErrorMessage = "Position must be a positive number.")] public int? OverallRank { get; set; }
    public string Status { get; set; } = "Finished";
    public bool IsPublished { get; set; } = true;
}

// ---------- Page models (what a page needs to display) ----------

public class HomeVm
{
    public List<EventDto> Events { get; set; } = new();
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
    public DateTime From { get; set; } = DateTime.Today;
    public string? ErrorMessage { get; set; }
}

public class EventDetailsVm
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public EnrolmentDto? MyEnrolment { get; set; }
}

public class DashboardVm
{
    public List<OrganiserEventDto> Events { get; set; } = new();
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public int UpcomingCount => Events.Count(e => e.EventDate >= DateOnly.FromDateTime(DateTime.Today));
}

public class ProfileVm
{
    public UserDto User { get; set; } = new();
    public ProfileForm Form { get; set; } = new();
}

public class CategoriesVm
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryForm Form { get; set; } = new();
}

public class EnrolmentsPageVm
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultRow
{
    public EnrolmentDto Enrolment { get; set; } = new();
    public ResultDto? Result { get; set; }
    public ResultForm Form { get; set; } = new();
}

public class ResultsPageVm
{
    public EventDto Event { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();
}

public class ErrorVm
{
    public int Code { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
}
