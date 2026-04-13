using System.Net.Http;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using LiveSessionService.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Infrastructure.Services.Cloudinary;

public sealed class CloudinaryMediaStorage : ICloudinaryMediaStorage
{
    private readonly CloudinaryDotNet.Cloudinary _cloudinary;
    private readonly string _cloudName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CloudinaryMediaStorage> _logger;

    public CloudinaryMediaStorage(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<CloudinaryMediaStorage> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")
            ?? configuration["Cloudinary:CloudName"];
        var apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")
            ?? configuration["Cloudinary:ApiKey"];
        var apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
            ?? configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName)
            || string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException("Cloudinary is not configured. Set CLOUDINARY_CLOUD_NAME/CLOUDINARY_API_KEY/CLOUDINARY_API_SECRET.");
        }

        _cloudName = cloudName;
        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new CloudinaryDotNet.Cloudinary(account);
    }

    public async Task<CloudinaryUploadResult> UploadAudioAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = "soundmates/live-session/audio",
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        if (result.StatusCode is not System.Net.HttpStatusCode.OK and not System.Net.HttpStatusCode.Created
            || string.IsNullOrWhiteSpace(result.SecureUrl?.ToString())
            || string.IsNullOrWhiteSpace(result.PublicId))
        {
            _logger.LogError("Cloudinary audio upload failed. Error={Error}", result.Error?.Message);
            throw new InvalidOperationException(result.Error?.Message ?? "Cloudinary audio upload failed.");
        }

        return new CloudinaryUploadResult(result.SecureUrl!.ToString(), result.PublicId);
    }

    public async Task<CloudinaryUploadResult?> UploadImageAsync(byte[] imageBytes, string fileName, CancellationToken cancellationToken = default)
    {
        await using var ms = new MemoryStream(imageBytes);

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, ms),
            Folder = "soundmates/live-session/artworks",
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        if (result.StatusCode is not System.Net.HttpStatusCode.OK and not System.Net.HttpStatusCode.Created
            || string.IsNullOrWhiteSpace(result.PublicId))
        {
            _logger.LogWarning("Cloudinary artwork upload failed. Error={Error}", result.Error?.Message);
            return null;
        }

        return new CloudinaryUploadResult(result.SecureUrl?.ToString() ?? string.Empty, result.PublicId);
    }

    public async Task<Stream> DownloadAudioAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException("PublicId cannot be null or empty", nameof(publicId));

        try
        {
            // Use Cloudinary SDK to build the download URL (supports signed URLs if needed)
            var downloadUrl = new Url(_cloudName)
                .ResourceType("raw")
                .Source(publicId)
                .BuildUrl();

            _logger.LogInformation(
                "Downloading audio from Cloudinary. PublicId={PublicId}, Url={Url}",
                publicId, downloadUrl);

            var client = _httpClientFactory.CreateClient("CloudinaryDownload");
            var response = await client.GetAsync(downloadUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Cloudinary audio download failed. PublicId={PublicId}, Status={Status}, Body={Body}",
                    publicId, response.StatusCode, body);
                throw new InvalidOperationException(
                    $"Failed to download audio from Cloudinary. Status: {response.StatusCode}");
            }

            var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            _logger.LogInformation(
                "Successfully downloaded audio from Cloudinary. PublicId={PublicId}, Size={Size}",
                publicId, memoryStream.Length);

            return memoryStream;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error downloading audio from Cloudinary. PublicId={PublicId}",
                publicId);
            throw new InvalidOperationException(
                $"Failed to download audio from Cloudinary. PublicId: {publicId}", ex);
        }
    }

    public async Task DeleteAudioAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return;

        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Raw
        });

        if (result.Result is not "ok" and not "not found")
        {
            _logger.LogWarning("Failed to delete Cloudinary audio {PublicId}. Result={Result}", publicId, result.Result);
        }
    }

    public async Task DeleteImageAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return;

        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image
        });

        if (result.Result is not "ok" and not "not found")
        {
            _logger.LogWarning("Failed to delete Cloudinary image {PublicId}. Result={Result}", publicId, result.Result);
        }
    }
}
