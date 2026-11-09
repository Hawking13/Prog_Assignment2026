using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

public class CategoriesController : ApiBase
{
    private readonly RaceDayDbContext _db;
    public CategoriesController(RaceDayDbContext db) => _db = db;

    private static CategoryResponse ToResponse(Category c) =>
        new(c.CategoryID, c.EventID, c.Name, c.DistanceKm, c.MinAge, c.MaxAge, c.Gender);

    /// <summary>Lists the categories of an event. Public.</summary>
    /// <response code="200">List of categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId)) return Missing("Event not found.");
        var cats = await _db.Categories.Where(c => c.EventID == eventId).OrderBy(c => c.Name).ToListAsync();
        return Ok(cats.Select(ToResponse).ToList());
    }

    /// <summary>Creates an age or distance category for an event. Event owner only.</summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed (e.g. MinAge greater than MaxAge).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">A category with that name already exists for this event.</response>
    [HttpPost("api/events/{eventId:int}/categories")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int eventId, CategoryDto dto)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return Missing("Event not found.");
        if (ev.OrganiserID != CurrentUserId) return Forbidden("You can only add categories to your own events.");
        if (dto.MinAge > dto.MaxAge) return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });
        if (await _db.Categories.AnyAsync(c => c.EventID == eventId && c.Name == dto.Name.Trim()))
            return Conflict(new { message = "A category with that name already exists for this event." });

        var c = new Category
        {
            EventID = eventId, Name = dto.Name.Trim(), DistanceKm = dto.DistanceKm,
            MinAge = dto.MinAge, MaxAge = dto.MaxAge, Gender = dto.Gender
        };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        return Created($"/api/events/{eventId}/categories", ToResponse(c));
    }

    /// <summary>Updates a category. Event owner only.</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("api/categories/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryDto dto)
    {
        var c = await _db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.CategoryID == id);
        if (c == null) return Missing("Category not found.");
        if (c.Event.OrganiserID != CurrentUserId) return Forbidden("You can only change categories of your own events.");
        if (dto.MinAge > dto.MaxAge) return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });

        c.Name = dto.Name.Trim(); c.DistanceKm = dto.DistanceKm; c.MinAge = dto.MinAge;
        c.MaxAge = dto.MaxAge; c.Gender = dto.Gender;
        await _db.SaveChangesAsync();
        return Ok(ToResponse(c));
    }

    /// <summary>Deletes a category. Event owner only; not allowed while participants are enrolled in it.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Participants are still enrolled in this category.</response>
    [HttpDelete("api/categories/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.CategoryID == id);
        if (c == null) return Missing("Category not found.");
        if (c.Event.OrganiserID != CurrentUserId) return Forbidden("You can only delete categories of your own events.");
        if (await _db.Registrations.AnyAsync(r => r.CategoryID == id))
            return Conflict(new { message = "Participants are enrolled in this category, so it cannot be deleted." });

        _db.Categories.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
