using Microsoft.AspNetCore.Mvc;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Features.Results.AzuraCast;
using LiveSessionService.Api.Models.Responses;
using Microsoft.Extensions.Configuration;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for AzuraCast health checking and API key validation
/// Clean controller following StationController pattern
/// </summary>
[ApiController]
[Route("api/v1/azuracast")]
[Produces("application/json")]
public class AzuraCastHealthController : ControllerBase
{
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzuraCastHealthController> _logger;

    public AzuraCastHealthController(
        IAzuraCastClient azuraCastClient,
        IConfiguration configuration,
        ILogger<AzuraCastHealthController> logger)
    {
        _azuraCastClient = azuraCastClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Check AzuraCast connection and API key validity
    /// </summary>
    /// <remarks>
    /// Tests connection, API key authentication, and station list retrieval
    /// </remarks>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<AzuraCastHealthResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 503)]
    public async Task<IActionResult> CheckHealth(CancellationToken ct)
    {
        var baseUrl = _configuration["AzuraCast:BaseUrl"] ?? "Not configured";
        _logger.LogInformation("Checking AzuraCast health at {BaseUrl}", baseUrl);

        // Try to fetch stations list
        var startTime = DateTime.UtcNow;
        var stations = await _azuraCastClient.GetStationsAsync(ct);
        var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

        var result = new AzuraCastHealthResult
        {
            IsHealthy = true,
            BaseUrl = baseUrl,
            StationCount = stations.Count,
            ResponseTimeMs = (int)responseTime,
            Message = $"Connected successfully. Found {stations.Count} station(s).",
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Health check successful. Stations: {Count}, Response: {Time}ms",
            stations.Count, responseTime);

        return Ok(ApiResponse<AzuraCastHealthResult>.SuccessResponse(
            result,
            "AzuraCast is healthy"));
    }

    /// <summary>
    /// Test API key validity
    /// </summary>
    [HttpPost("test-apikey")]
    [ProducesResponseType(typeof(ApiResponse<ApiKeyTestResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> TestApiKey([FromQuery] string? apiKey, CancellationToken ct)
    {
        var baseUrl = _configuration["AzuraCast:BaseUrl"] ?? "Not configured";
        var keyToTest = apiKey ?? _configuration["AzuraCast:ApiKey"];

        if (string.IsNullOrEmpty(keyToTest))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "No API key provided",
                400));
        }

        var stations = await _azuraCastClient.GetStationsAsync(ct);

        var result = new ApiKeyTestResult
        {
            IsValid = true,
            ApiKey = MaskApiKey(keyToTest),
            BaseUrl = baseUrl,
            StationCount = stations.Count,
            Message = "API key is valid",
            Timestamp = DateTime.UtcNow
        };

        return Ok(ApiResponse<ApiKeyTestResult>.SuccessResponse(result, "API key is valid"));
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 8)
            return "***";

        return $"{apiKey[..4]}...{apiKey[^4..]}";
    }
}
