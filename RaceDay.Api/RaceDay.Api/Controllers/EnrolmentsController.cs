using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

// An "enrolment" is stored in the Registration table (Participant + Category, and so the Event).
public class EnrolmentsController : ApiBase
{
    private readonly RaceDayDbContext _db;
    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    private static EnrolmentResponse ToResponse(Registration r) =>
        new(r.RegistrationID, r.Category.EventID, r.Category.Event.Name, r.CategoryID, r.Category.Name,
            r.ParticipantID, r.Participant.Name, r.BibNumber, r.RegistrationDate);

    private IQueryable<Registration> WithDetails() =>
        _db.Registrations.Include(r => r.Category).ThenInclude(c => c.Event).Include(r => r.Participant);

    /// <summary>Enters the logged-in Participant into an event by choosing a category.</summary>
    /// <remarks>Records the link between Participant, Event and Category and assigns a bib number.</remarks>
    /// <response code="201">Enrolment recorded.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant.</response>
    /// <response code="404">Event, or that category within the event, not found.</response>
    /// <response code="409">Already enrolled in this event.</response>
    [HttpPost("api/events/{eventId:int}/enrol")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int eventId, EnrolDto dto)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryID == dto.CategoryId && c.EventID == eventId);
        if (category == null) return Missing("Event or category not found.");

        var already = await _db.Registrations.AnyAsync(r => r.ParticipantID == CurrentUserId && r.Category.EventID == eventId);
        if (already) return Conflict(new { message = "You are already enrolled in this event." });

        // Generate a bib number that is unique across the table
        var n = await _db.Registrations.CountAsync(r => r.Category.EventID == eventId) + 1;
        string bib;
        do { bib = $"E{eventId}-{n:D4}"; n++; }
        while (await _db.Registrations.AnyAsync(r => r.BibNumber == bib));

        var reg = new Registration
        {
            ParticipantID = CurrentUserId, CategoryID = category.CategoryID,
            BibNumber = bib, RegistrationDate = DateTime.Now
        };
        _db.Registrations.Add(reg);
        await _db.SaveChangesAsync();

        var saved = await WithDetails().FirstAsync(r => r.RegistrationID == reg.RegistrationID);
        return Created("/api/users/me/enrolments", ToResponse(saved));
    }

    /// <summary>Lists everyone enrolled in an event. Only the Organiser who owns the event.</summary>
    /// <response code="200">List of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return Missing("Event not found.");
        if (ev.OrganiserID != CurrentUserId) return Forbidden("You can only view enrolments for your own events.");

        var regs = await WithDetails().Where(r => r.Category.EventID == eventId).OrderBy(r => r.BibNumber).ToListAsync();
        return Ok(regs.Select(ToResponse).ToList());
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    /// <response code="200">List of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant.</response>
    [HttpGet("api/users/me/enrolments")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var regs = await WithDetails().Where(r => r.ParticipantID == CurrentUserId)
            .OrderByDescending(r => r.RegistrationDate).ToListAsync();
        return Ok(regs.Select(ToResponse).ToList());
    }

    /// <summary>Withdraws the logged-in Participant from an event (deletes their own enrolment).</summary>
    /// <response code="204">Enrolment removed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant, or not your enrolment.</response>
    /// <response code="404">Enrolment not found.</response>
    [HttpDelete("api/enrolments/{id:int}")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Withdraw(int id)
    {
        var reg = await _db.Registrations.Include(r => r.Result).FirstOrDefaultAsync(r => r.RegistrationID == id);
        if (reg == null) return Missing("Enrolment not found.");
        if (reg.ParticipantID != CurrentUserId) return Forbidden("You can only withdraw your own enrolment.");

        _db.Registrations.Remove(reg);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
