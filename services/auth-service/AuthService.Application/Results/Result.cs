namespace AuthService.Application.Results;

/// <summary>
/// Generic result pattern for Application layer
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public int? ErrorCode { get; init; }

    public static Result<T> Success(T data, string? message = null) 
        => new() 
        { 
            IsSuccess = true, 
            Data = data, 
            ErrorMessage = message 
        };

    public static Result<T> Failure(string errorMessage, int? errorCode = null) 
        => new() 
        { 
            IsSuccess = false, 
            ErrorMessage = errorMessage, 
            ErrorCode = errorCode 
        };
}

/// <summary>
/// Result without data
/// </summary>
public class Result
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public int? ErrorCode { get; init; }

    public static Result Success() 
        => new() { IsSuccess = true };

    public static Result Failure(string errorMessage, int? errorCode = null) 
        => new() 
        { 
            IsSuccess = false, 
            ErrorMessage = errorMessage, 
            ErrorCode = errorCode 
        };
}
