using System.Text.Json.Serialization;
using AuthService.Application.Enums;

namespace AuthService.Api.Models.Responses;

/// <summary>
/// HTTP response wrapper — all API responses use this consistent structure.
/// JSON uses camelCase to match frontend conventions.
/// </summary>
public class ApiResponse<T>
{
    /// <summary>Indicates whether the request succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Human-readable message (usually in Vietnamese).</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>Response payload. Null on failure.</summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>HTTP status code of the error. Null on success.</summary>
    [JsonPropertyName("errorCode")]
    public int? ErrorCode { get; set; }

    /// <summary>Server-side trace ID for debugging.</summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    /// <summary>ISO 8601 timestamp when the response was generated.</summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    // ── Factory methods ────────────────────────────────────────────

    /// <summary>Creates a success response.</summary>
    public static ApiResponse<T> SuccessResponse(
        T data,
        string? message = null,
        string? requestId = null)
        => new()
        {
            Success = true,
            Data = data,
            Message = message,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

    /// <summary>Creates a failure response.</summary>
    public static ApiResponse<T> FailureResponse(
        string message,
        int? errorCode = null,
        string? requestId = null)
        => new()
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

    /// <summary>Creates a failure from an ApiStatusCode enum.</summary>
    public static ApiResponse<T> FromStatusCode(
        ApiStatusCode code,
        string? customMessage = null,
        string? requestId = null)
        => new()
        {
            Success = false,
            Message = customMessage ?? code.ToString(),
            ErrorCode = (int)code,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

    /// <summary>Maps from an Application Result{T}.</summary>
    public static ApiResponse<T> FromResult(
        Application.Results.Result<T> result,
        string? requestId = null)
        => new()
        {
            Success = false,
            Message = result.ErrorMessage,
            ErrorCode = result.ErrorCode,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

    /// <summary>Maps from a non-generic Application Result.</summary>
    public static ApiResponse<T> FromVoidResult(
        Application.Results.Result voidResult,
        string? requestId = null)
        => new()
        {
            Success = false,
            Message = voidResult.ErrorMessage,
            ErrorCode = voidResult.ErrorCode,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

    /// <summary>
    /// Backward-compatible alias for FromStatusCode.
    /// </summary>
    [Obsolete("Use FromStatusCode or FailureResponse instead")]
    public static ApiResponse<T> Error(ApiStatusCode code, string message)
        => FromStatusCode(code, message);
}
