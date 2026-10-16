using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

public class EventsController : ApiBase
{
    private readonly RaceDayDbContext _db;
    public EventsController(RaceDayDbContext db) => _db = db;

    private static EventResponse ToResponse(Event e) =>
        new(e.EventID, e.OrganiserID, e.Name, e.Description, e.EventDate, e.Location, e.DistanceKm, e.EventType);

    /// <summary>Lists all events. Public. Optional filters: type, location, from (date).</summary>
    /// <response code="200">List of events, soonest first.</response>
    [HttpGet("api/events")]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] string? location, [FromQuery] DateOnly? from)
    {
        var q = _db.Events.AsQueryable();
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(e => e.EventType == type);
        if (!string.IsNullOrWhiteSpace(location)) q = q.Where(e => e.Location.Contains(location));
        if (from != null) q = q.Where(e => e.EventDate >= from);

        var events = await q.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(ToResponse).ToList());
    }

    /// <summary>Returns one event by id. Public.</summary>
    /// <response code="200">Event details.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await _db.Events.FindAsync(id);
        return e == null ? Missing("Event not found.") : Ok(ToResponse(e));
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    [HttpPost("api/events")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventDto dto)
    {
        if (dto.EventDate == default) return BadRequest(new { message = "EventDate is required (yyyy-MM-dd)." });

        var e = new Event
        {
            OrganiserID = CurrentUserId, Name = dto.Name.Trim(), Description = dto.Description,
            EventDate = dto.EventDate, Location = dto.Location.Trim(), DistanceKm = dto.DistanceKm, EventType = dto.EventType
        };
        _db.Events.Add(e);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = e.EventID }, ToResponse(e));
    }

    /// <summary>Updates an event. Only the Organiser who created it may do this.</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("api/events/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventDto dto)
    {
        var e = await _db.Events.FindAsync(id);
        if (e == null) return Missing("Event not found.");
        if (e.OrganiserID != CurrentUserId) return Forbidden("You can only change your own events.");
        if (dto.EventDate == default) return BadRequest(new { message = "EventDate is required (yyyy-MM-dd)." });

        e.Name = dto.Name.Trim(); e.Description = dto.Description; e.EventDate = dto.EventDate;
        e.Location = dto.Location.Trim(); e.DistanceKm = dto.DistanceKm; e.EventType = dto.EventType;
        await _db.SaveChangesAsync();
        return Ok(ToResponse(e));
    }

    /// <summary>Deletes an event (and its categories, enrolments and results). Owner only.</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpDelete("api/events/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Events.Include(x => x.Categories).FirstOrDefaultAsync(x => x.EventID == id);
        if (e == null) return Missing("Event not found.");
        if (e.OrganiserID != CurrentUserId) return Forbidden("You can only delete your own events.");

        // Registrations point at categories with "no action" on delete, so remove them first.
        var regs = await _db.Registrations.Include(r => r.Result)
            .Where(r => r.Category.EventID == id).ToListAsync();
        _db.Registrations.RemoveRange(regs);
        _db.Events.Remove(e);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
