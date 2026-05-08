namespace AiService.Application.Interfaces;

public interface IAudioConversionService
{
    Task<(bool IsSuccess, byte[]? Mp3Bytes, string? ErrorMessage)> ConvertToMp3Async(
        byte[] inputBytes,
        string inputExtension,
        CancellationToken cancellationToken = default);

    Task<(bool IsSuccess, byte[]? MixedBytes, string? ErrorMessage)> MixWithBackgroundMusicAsync(
        byte[] speechBytes,
        string speechExtension,
        string bgmUrlOrPath,
        CancellationToken cancellationToken = default);

    Task<(bool IsSuccess, byte[]? ConcatenatedBytes, string? ErrorMessage)> ConcatenateAudiosAsync(
        List<byte[]> audioChunks,
        string extension,
        CancellationToken cancellationToken = default);
}
