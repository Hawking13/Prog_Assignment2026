using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

public class ProfileController : ApiBase
{
    private readonly RaceDayDbContext _db;
    public ProfileController(RaceDayDbContext db) => _db = db;

    /// <summary>Returns the logged-in user's own profile.</summary>
    /// <response code="200">Profile returned.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("api/users/me")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var role = HttpContext.Session.GetRole();
        if (role == Roles.Organiser)
        {
            var o = await _db.Organisers.FindAsync(CurrentUserId);
            if (o == null) return Missing("User not found.");
            return Ok(new UserResponse(o.OrganiserID, o.Name, o.Email, Roles.Organiser, o.Phone, null, null));
        }
        var p = await _db.Participants.FindAsync(CurrentUserId);
        if (p == null) return Missing("User not found.");
        return Ok(new UserResponse(p.ParticipantID, p.Name, p.Email, Roles.Participant, p.Phone, p.DateOfBirth, p.Gender));
    }

    /// <summary>Updates the logged-in user's own name, email and phone.</summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="409">Email already used by another account.</response>
    [HttpPut("api/users/me")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMe(ProfileUpdateDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var role = HttpContext.Session.GetRole();
        var id = CurrentUserId;

        // Email must be unique across BOTH account tables (excluding the user themself)
        var takenByOrganiser = await _db.Organisers.AnyAsync(o => o.Email == email && !(role == Roles.Organiser && o.OrganiserID == id));
        var takenByParticipant = await _db.Participants.AnyAsync(p => p.Email == email && !(role == Roles.Participant && p.ParticipantID == id));
        if (takenByOrganiser || takenByParticipant)
            return Conflict(new { message = "That email is already used by another account." });

        if (role == Roles.Organiser)
        {
            var o = await _db.Organisers.FindAsync(id);
            if (o == null) return Missing("User not found.");
            o.Name = dto.Name.Trim(); o.Email = email; o.Phone = dto.Phone;
            await _db.SaveChangesAsync();
            return Ok(new UserResponse(o.OrganiserID, o.Name, o.Email, Roles.Organiser, o.Phone, null, null));
        }

        var p2 = await _db.Participants.FindAsync(id);
        if (p2 == null) return Missing("User not found.");
        p2.Name = dto.Name.Trim(); p2.Email = email; p2.Phone = dto.Phone;
        await _db.SaveChangesAsync();
        return Ok(new UserResponse(p2.ParticipantID, p2.Name, p2.Email, Roles.Participant, p2.Phone, p2.DateOfBirth, p2.Gender));
    }
}
