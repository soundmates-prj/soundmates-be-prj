using AiService.Application.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;

namespace AiService.Infrastructure.Storage;

public sealed class CloudinaryAudioStorage : ICloudinaryAudioStorage
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryAudioStorage> _logger;
    private const string Folder = "soundmates/ai-service/audio";

    public CloudinaryAudioStorage(
        Cloudinary cloudinary,
        ILogger<CloudinaryAudioStorage> logger)
    {
        _cloudinary = cloudinary;
        _logger = logger;
    }

    public async Task<CloudinaryUploadResult> UploadAudioAsync(
        byte[] bytes,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(bytes);
        return await UploadAudioAsync(stream, fileName, cancellationToken);
    }

    public async Task<CloudinaryUploadResult> UploadAudioAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = Folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.StatusCode is not System.Net.HttpStatusCode.OK
            and not System.Net.HttpStatusCode.Created)
        {
            _logger.LogError("Cloudinary upload failed: {Error}", result.Error?.Message);
            throw new InvalidOperationException(
                result.Error?.Message ?? "Cloudinary audio upload failed.");
        }

        if (string.IsNullOrWhiteSpace(result.SecureUrl?.ToString())
            || string.IsNullOrWhiteSpace(result.PublicId))
        {
            _logger.LogError("Cloudinary returned empty URL or PublicId");
            throw new InvalidOperationException(
                "Cloudinary upload succeeded but returned no URL/PublicId.");
        }

        _logger.LogInformation(
            "Uploaded audio to Cloudinary. PublicId={PublicId}, Url={Url}",
            result.PublicId, result.SecureUrl);

        return new CloudinaryUploadResult(
            Url: result.SecureUrl.ToString(),
            PublicId: result.PublicId);
    }

    public async Task DeleteAudioAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return;

        var result = await _cloudinary.DeleteResourcesAsync(new[] { publicId });

        if (result.StatusCode is not System.Net.HttpStatusCode.OK
            and not System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Cloudinary delete failed for PublicId={PublicId}: {Error}",
                publicId, result.Error?.Message);
        }
        else
        {
            _logger.LogInformation("Deleted audio from Cloudinary: {PublicId}", publicId);
        }
    }
}
