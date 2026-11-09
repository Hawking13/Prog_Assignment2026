using Azure;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

// All image handling goes through the API; the MVC app never talks to Azure directly.
public class UploadsController : ApiBase
{
    private readonly RaceDayDbContext _db;
    private readonly IImageStorage _storage;
    public UploadsController(RaceDayDbContext db, IImageStorage storage) { _db = db; _storage = storage; }

    /// <summary>Uploads a banner image for an event (stored in Azure Blob Storage). Event owner only.</summary>
    /// <remarks>Send as multipart/form-data with a file field named "file". JPG, PNG, WEBP or GIF, max 5 MB.</remarks>
    /// <response code="200">Banner saved; the image URL is returned.</response>
    /// <response code="400">Missing, too large, or not an image.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="502">Azure storage rejected the upload.</response>
    /// <response code="503">Image storage is not configured.</response>
    [HttpPost("api/events/{id:int}/banner")]
    [SessionAuthorize(Roles.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return Missing("Event not found.");
        if (ev.OrganiserID != CurrentUserId) return Forbidden("You can only change the banner of your own events.");

        var error = ImageRules.Validate(file);
        if (error != null) return BadRequest(new { message = error });

        try { ev.BannerUrl = await _storage.UploadAsync(file, "banners"); }
        catch (InvalidOperationException ex) { return StatusCode(503, new { message = ex.Message }); }
        catch (RequestFailedException) { return StatusCode(502, new { message = "Image storage failed. Please try again later." }); }

        await _db.SaveChangesAsync();
        return Ok(new ImageUploadResponse(ev.BannerUrl));
    }

    /// <summary>Uploads the logged-in Participant's profile picture (stored in Azure Blob Storage).</summary>
    /// <remarks>Send as multipart/form-data with a file field named "file". JPG, PNG, WEBP or GIF, max 5 MB.</remarks>
    /// <response code="200">Picture saved; the image URL is returned.</response>
    /// <response code="400">Missing, too large, or not an image.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant.</response>
    /// <response code="502">Azure storage rejected the upload.</response>
    /// <response code="503">Image storage is not configured.</response>
    [HttpPost("api/users/me/picture")]
    [SessionAuthorize(Roles.Participant)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        var p = await _db.Participants.FindAsync(CurrentUserId);
        if (p == null) return Missing("User not found.");

        var error = ImageRules.Validate(file);
        if (error != null) return BadRequest(new { message = error });

        try { p.ProfilePictureUrl = await _storage.UploadAsync(file, "profiles"); }
        catch (InvalidOperationException ex) { return StatusCode(503, new { message = ex.Message }); }
        catch (RequestFailedException) { return StatusCode(502, new { message = "Image storage failed. Please try again later." }); }

        await _db.SaveChangesAsync();
        return Ok(new ImageUploadResponse(p.ProfilePictureUrl));
    }
}
