namespace LiveSessionService.Api.Models.Responses;

/// <summary>
/// Standard API response wrapper
/// Used at API/Controller layer only
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public int? ErrorCode { get; init; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        => new()
        {
            Success = true,
            Data = data,
            Message = message
        };

    public static ApiResponse<T> FailureResponse(string message, int? errorCode = null)
        => new()
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
}
