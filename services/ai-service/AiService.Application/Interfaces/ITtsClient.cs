namespace AiService.Application.Interfaces;

public record TtsSynthesizeRequest(
    string Text,
    string VoiceCode,
    string? Model,
    decimal? Speed,
    decimal? Pitch);

public record TtsSynthesizeResponse(
    byte[] AudioBytes,
    string ContentType,
    int? DurationSeconds = null,
    int? TokensUsed = null,
    decimal? Cost = null,
    string? RawProviderResponse = null);

public interface ITtsClient
{
    Task<TtsSynthesizeResponse> SynthesizeAsync(TtsSynthesizeRequest request, CancellationToken cancellationToken);
}

