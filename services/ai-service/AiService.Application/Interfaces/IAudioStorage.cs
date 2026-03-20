namespace AiService.Application.Interfaces;

public record StoredAudioFile(
    string RelativePath,
    long SizeBytes,
    string ContentType);

public interface IAudioStorage
{
    Task<StoredAudioFile> SaveAsync(
        string fileNameWithoutExtension,
        string extensionWithDot,
        string contentType,
        byte[] bytes,
        CancellationToken cancellationToken);

    Task<(Stream Stream, string ContentType, long? ContentLength)> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken);
}

