using AiService.Application.Results;

namespace AiService.Application.Interfaces;

public record TextToSpeechRequest(
    string Text,
    string Voice,
    bool IncludeAudioBytes = false);

public record TextToSpeechResult(
    string? AudioUrl,
    byte[]? AudioBytes,
    int? Duration,
    string ContentType,
    string Provider);

public interface ITextToSpeechService
{
    Task<Result<TextToSpeechResult>> GenerateAudioAsync(TextToSpeechRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> CloneVoiceAsync(string voiceId, string refText, byte[] audioBytes, string fileName, CancellationToken cancellationToken);
}
