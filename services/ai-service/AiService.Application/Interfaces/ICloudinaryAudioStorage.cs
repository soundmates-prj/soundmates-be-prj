namespace AiService.Application.Interfaces;

public sealed record CloudinaryUploadResult(string Url, string PublicId);

public interface ICloudinaryAudioStorage
{
    Task<CloudinaryUploadResult> UploadAudioAsync(byte[] bytes, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAudioAsync(string publicId, CancellationToken cancellationToken = default);
}