using AiService.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace AiService.Infrastructure.Storage;

/// <summary>
/// Fallback when Cloudinary is not configured — stores audio locally via LocalAudioStorage
/// but implements ICloudinaryAudioStorage so the DI composition stays identical.
/// Constructs a full public URL so the audio is playable from the browser.
/// </summary>
public sealed class LocalFallbackCloudinaryStorage : ICloudinaryAudioStorage
{
    private readonly IAudioStorage _localStorage;
    private readonly StorageOptions _options;

    public LocalFallbackCloudinaryStorage(IAudioStorage localStorage, IOptions<StorageOptions> options)
    {
        _localStorage = localStorage;
        _options = options.Value;
    }

    public async Task<CloudinaryUploadResult> UploadAudioAsync(
        byte[] bytes,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName);

        var stored = await _localStorage.SaveAsync(
            fileNameWithoutExtension: Guid.NewGuid().ToString("N"),
            extensionWithDot: ".mp3",
            contentType: contentType,
            bytes: bytes,
            cancellationToken: cancellationToken);

        // Build full public URL so the browser can play it directly.
        // LocalAudioStorage saves to: {AudioRoot}/{guid}.mp3
        // Served by: {PublicBaseUrl}/api/audios/podcast-file/{guid}.mp3
        var baseUrl = ResolvePublicBaseUrl();
        var publicUrl = $"{baseUrl.TrimEnd('/')}/api/audios/podcast-file/{stored.RelativePath}";

        return new CloudinaryUploadResult(
            Url: publicUrl,
            PublicId: stored.RelativePath);
    }

    public async Task DeleteAudioAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(publicId))
        {
            await _localStorage.DeleteAsync(publicId, cancellationToken);
        }
    }

    private string ResolvePublicBaseUrl()
    {
        var configured = _options.PublicBaseUrl;
        if (!string.IsNullOrWhiteSpace(configured)
            && !configured.StartsWith("${")
            && Uri.TryCreate(configured, UriKind.Absolute, out _))
        {
            return configured.TrimEnd('/');
        }

        // Fallback: use container/Docker host internal URL
        return "http://host.docker.internal:8080";
    }
}