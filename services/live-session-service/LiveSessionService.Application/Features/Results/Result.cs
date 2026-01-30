namespace LiveSessionService.Application.Features.Results;

/// <summary>
/// Result pattern for application layer
/// Separates success/failure from HTTP concerns
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorMessage { get; }
    public int? ErrorCode { get; }

    protected Result(bool isSuccess, string? errorMessage = null, int? errorCode = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true);
    public static Result Failure(string errorMessage, int? errorCode = null) => new(false, errorMessage, errorCode);
}

/// <summary>
/// Result pattern with data
/// </summary>
public class Result<T> : Result
{
    public T? Data { get; }

    private Result(bool isSuccess, T? data, string? errorMessage = null, int? errorCode = null)
        : base(isSuccess, errorMessage, errorCode)
    {
        Data = data;
    }

    public static Result<T> Success(T data) => new(true, data);
    public static Result<T> Failure(string errorMessage, int? errorCode = null) => new(false, default, errorMessage, errorCode);
}
