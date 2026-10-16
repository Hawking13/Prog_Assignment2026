using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

public class AuthController : ApiBase
{
    private readonly RaceDayDbContext _db;
    public AuthController(RaceDayDbContext db) => _db = db;

    /// <summary>Registers a new Organiser or Participant account.</summary>
    /// <remarks>Participants must also supply DateOfBirth and Gender. The password is hashed before it is stored.</remarks>
    /// <response code="201">Account created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("api/auth/register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (dto.Role == Roles.Participant && (dto.DateOfBirth is null || dto.Gender is null))
            return BadRequest(new { message = "Participants must supply DateOfBirth and Gender." });

        if (await _db.Organisers.AnyAsync(o => o.Email == email) ||
            await _db.Participants.AnyAsync(p => p.Email == email))
            return Conflict(new { message = "That email is already registered." });

        var hash = PasswordHelper.Hash(dto.Password);

        if (dto.Role == Roles.Organiser)
        {
            var org = new Organiser { Name = dto.Name.Trim(), Email = email, Phone = dto.Phone, PasswordHash = hash };
            _db.Organisers.Add(org);
            await _db.SaveChangesAsync();
            return Created("/api/users/me",
                new UserResponse(org.OrganiserID, org.Name, org.Email, Roles.Organiser, org.Phone, null, null));
        }

        var p = new Participant
        {
            Name = dto.Name.Trim(), Email = email, Phone = dto.Phone,
            DateOfBirth = dto.DateOfBirth!.Value, Gender = dto.Gender!, PasswordHash = hash
        };
        _db.Participants.Add(p);
        await _db.SaveChangesAsync();
        return Created("/api/users/me",
            new UserResponse(p.ParticipantID, p.Name, p.Email, Roles.Participant, p.Phone, p.DateOfBirth, p.Gender));
    }

    /// <summary>Logs in and starts a session that remembers the user's id and role.</summary>
    /// <response code="200">Logged in. A session cookie is returned.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("api/auth/login")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var org = await _db.Organisers.FirstOrDefaultAsync(o => o.Email == email);
        if (org != null && PasswordHelper.Verify(dto.Password, org.PasswordHash))
        {
            HttpContext.Session.Clear();
            HttpContext.Session.SetUser(org.OrganiserID, Roles.Organiser);
            return Ok(new UserResponse(org.OrganiserID, org.Name, org.Email, Roles.Organiser, org.Phone, null, null));
        }

        var p = await _db.Participants.FirstOrDefaultAsync(x => x.Email == email);
        if (p != null && PasswordHelper.Verify(dto.Password, p.PasswordHash))
        {
            HttpContext.Session.Clear();
            HttpContext.Session.SetUser(p.ParticipantID, Roles.Participant);
            return Ok(new UserResponse(p.ParticipantID, p.Name, p.Email, Roles.Participant, p.Phone, p.DateOfBirth, p.Gender));
        }

        return Unauthorized(new { message = "Invalid email or password." });
    }

    /// <summary>Logs out by clearing the session.</summary>
    /// <response code="204">Logged out.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("api/auth/logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }
}
