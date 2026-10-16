using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

public class ResultsController : ApiBase
{
    private readonly RaceDayDbContext _db;
    public ResultsController(RaceDayDbContext db) => _db = db;

    private static ResultResponse ToResponse(Result r) =>
        new(r.ResultID, r.RegistrationID, r.Registration.Category.Event.Name, r.Registration.Participant.Name,
            r.Registration.Category.Name, r.FinishTime, r.OverallRank, r.Status);

    private IQueryable<Result> WithDetails() =>
        _db.Results.Include(r => r.Registration).ThenInclude(g => g.Category).ThenInclude(c => c.Event)
                   .Include(r => r.Registration).ThenInclude(g => g.Participant);

    /// <summary>Captures a finish time and position for an enrolment. Event owner only.</summary>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment (use PUT to change it).</response>
    [HttpPost("api/enrolments/{id:int}/result")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(int id, ResultDto dto)
    {
        var reg = await _db.Registrations.Include(r => r.Category).ThenInclude(c => c.Event)
            .Include(r => r.Result).FirstOrDefaultAsync(r => r.RegistrationID == id);
        if (reg == null) return Missing("Enrolment not found.");
        if (reg.Category.Event.OrganiserID != CurrentUserId) return Forbidden("You can only capture results for your own events.");
        if (reg.Result != null) return Conflict(new { message = "A result already exists for this enrolment." });

        var result = new Result { RegistrationID = id, FinishTime = dto.FinishTime, OverallRank = dto.OverallRank, Status = dto.Status };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var saved = await WithDetails().FirstAsync(r => r.ResultID == result.ResultID);
        return Created($"/api/events/{reg.Category.EventID}/results", ToResponse(saved));
    }

    /// <summary>Corrects an existing result. Event owner only.</summary>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("api/results/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, ResultDto dto)
    {
        var result = await WithDetails().FirstOrDefaultAsync(r => r.ResultID == id);
        if (result == null) return Missing("Result not found.");
        if (result.Registration.Category.Event.OrganiserID != CurrentUserId)
            return Forbidden("You can only change results for your own events.");

        result.FinishTime = dto.FinishTime; result.OverallRank = dto.OverallRank; result.Status = dto.Status;
        await _db.SaveChangesAsync();
        return Ok(ToResponse(result));
    }

    /// <summary>Returns all results for an event (public leaderboard), best rank first.</summary>
    /// <response code="200">List of results.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId:int}/results")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId)) return Missing("Event not found.");
        var results = await WithDetails().Where(r => r.Registration.Category.EventID == eventId)
            .OrderBy(r => r.OverallRank == null).ThenBy(r => r.OverallRank).ToListAsync();
        return Ok(results.Select(ToResponse).ToList());
    }

    /// <summary>Returns the logged-in Participant's own results.</summary>
    /// <response code="200">List of results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant.</response>
    [HttpGet("api/users/me/results")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var results = await WithDetails().Where(r => r.Registration.ParticipantID == CurrentUserId).ToListAsync();
        return Ok(results.Select(ToResponse).ToList());
    }
}
