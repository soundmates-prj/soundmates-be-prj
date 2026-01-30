namespace LiveSessionService.Api.Models.Responses;

/// <summary>
/// Standard API response wrapper
/// Used at API/Controller layer only
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public int? ErrorCode { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        => new ApiResponse<T> { Success = true, Data = data, Message = message };

    public static ApiResponse<T> FailureResponse(string message, int? errorCode = null)
        => new ApiResponse<T> { Success = false, Message = message, ErrorCode = errorCode };
}
        => ApiResponse<object>.Error(apiCode, message);
}
