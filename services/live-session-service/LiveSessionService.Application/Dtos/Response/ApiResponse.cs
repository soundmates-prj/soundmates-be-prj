using LiveSessionService.Application.Enums;

namespace LiveSessionService.Application.Dtos.Response;

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

    public static ApiResponse<T> Error(ApiStatusCode apiCode, string message)
        => new ApiResponse<T> { Success = false, Message = message, ErrorCode = (int)apiCode };
}

public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T data, string? message = null)
        => ApiResponse<T>.SuccessResponse(data, message);
    
    public static ApiResponse<object> Success(string message)
        => ApiResponse<object>.SuccessResponse(new { }, message);
    
    public static ApiResponse<object> Error(string message, ApiStatusCode apiCode)
        => ApiResponse<object>.Error(apiCode, message);
}
