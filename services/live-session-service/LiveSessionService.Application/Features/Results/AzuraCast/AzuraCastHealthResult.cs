namespace LiveSessionService.Application.Features.Results.AzuraCast;

/// <summary>
/// AzuraCast health check result
/// </summary>
public sealed class AzuraCastHealthResult
{
    public bool IsHealthy { get; init; }
    public string BaseUrl { get; init; } = null!;
    public int StationCount { get; init; }
    public int ResponseTimeMs { get; init; }
    public string Message { get; init; } = null!;
    public string? Error { get; init; }
    public DateTime Timestamp { get; init; }
}
