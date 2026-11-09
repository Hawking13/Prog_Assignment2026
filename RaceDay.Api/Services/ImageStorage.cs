using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IImageStorage
{
    /// Uploads the image and returns its public URL.
    Task<string> UploadAsync(IFormFile file, string folder);
}

// All Azure Blob Storage access lives here. The MVC app never talks to Azure; it calls the API.
public class BlobImageStorage : IImageStorage
{
    private readonly IConfiguration _config;
    private BlobContainerClient? _container;

    public BlobImageStorage(IConfiguration config) => _config = config;

    private BlobContainerClient Container()
    {
        if (_container != null) return _container;
        var conn = _config["AzureStorage:ConnectionString"];
        var name = _config["AzureStorage:ContainerName"] ?? "raceday-images";
        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException("Image storage is not configured.");
        _container = new BlobContainerClient(conn, name);
        return _container;
    }

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        var container = Container();
        await container.CreateIfNotExistsAsync();

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blob = container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{ext}");
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
        return blob.Uri.ToString();
    }
}

public static class ImageRules
{
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxBytes = 5 * 1024 * 1024;

    /// Returns an error message, or null if the file is acceptable.
    public static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0) return "Please choose an image file.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext) || !AllowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return "Only JPG, PNG, WEBP or GIF images are allowed.";
        return null;
    }
}
