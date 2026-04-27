namespace LiveSessionService.Application.Features.Results.AzuraCast;

/// <summary>
/// API key test result
/// </summary>
public sealed class ApiKeyTestResult
{
    public bool IsValid { get; init; }
    public string ApiKey { get; init; } = null!; // Masked
    public string BaseUrl { get; init; } = null!;
    public int StationCount { get; init; }
    public string Message { get; init; } = null!;
    public string? Error { get; init; }
    public DateTime Timestamp { get; init; }
}
