namespace LiveSessionService.Application.Abstractions;

public sealed record CloudinaryUploadResult(string Url, string PublicId);

public interface ICloudinaryMediaStorage
{
    Task<CloudinaryUploadResult> UploadAudioAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    Task<CloudinaryUploadResult?> UploadImageAsync(byte[] imageBytes, string fileName, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAudioAsync(string publicId, CancellationToken cancellationToken = default);
    Task DeleteAudioAsync(string publicId, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(string publicId, CancellationToken cancellationToken = default);
}
