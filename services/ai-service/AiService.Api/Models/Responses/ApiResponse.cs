using AiService.Application.Enums;

namespace AiService.Api.Models.Responses;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public int? ErrorCode { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> FailureResponse(string message, int? errorCode = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode };

    public static ApiResponse<T> Error(ApiStatusCode apiCode, string message)
        => new() { Success = false, Message = message, ErrorCode = (int)apiCode };
}

