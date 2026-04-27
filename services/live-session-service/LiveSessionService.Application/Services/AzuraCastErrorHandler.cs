using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Exceptions;
using Microsoft.Extensions.Logging;
using System.Net;

namespace LiveSessionService.Application.Services;

/// <summary>
/// Service for handling and mapping AzuraCast errors to application errors
/// </summary>
public interface IAzuraCastErrorHandler
{
    ErrorCode MapHttpStatusToErrorCode(HttpStatusCode? statusCode);
    string GetUserFriendlyMessage(Exception exception);
    bool IsTransientError(Exception exception);
    bool ShouldRetry(Exception exception, int attemptNumber);
}

public sealed class AzuraCastErrorHandler : IAzuraCastErrorHandler
{
    private readonly ILogger<AzuraCastErrorHandler> _logger;

    public AzuraCastErrorHandler(ILogger<AzuraCastErrorHandler> logger)
    {
        _logger = logger;
    }

    public ErrorCode MapHttpStatusToErrorCode(HttpStatusCode? statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => ErrorCode.Unauthorized,
            HttpStatusCode.Forbidden => ErrorCode.Forbidden,
            HttpStatusCode.NotFound => ErrorCode.NotFound,
            HttpStatusCode.BadRequest => ErrorCode.BadRequest,
            HttpStatusCode.Conflict => ErrorCode.Conflict,
            HttpStatusCode.RequestTimeout => ErrorCode.RequestTimeout,
            HttpStatusCode.TooManyRequests => ErrorCode.TooManyRequests,
            HttpStatusCode.InternalServerError => ErrorCode.ServiceUnavailable,
            HttpStatusCode.BadGateway => ErrorCode.ServiceUnavailable,
            HttpStatusCode.ServiceUnavailable => ErrorCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout => ErrorCode.RequestTimeout,
            _ => ErrorCode.InternalServerError
        };
    }

    public string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            AzuraCastException azEx => GetAzuraCastErrorMessage(azEx),
            HttpRequestException httpEx => GetHttpErrorMessage(httpEx),
            TaskCanceledException => "The request to AzuraCast timed out. Please try again later.",
            OperationCanceledException => "The operation was cancelled.",
            _ => "An unexpected error occurred while communicating with AzuraCast."
        };
    }

    public bool IsTransientError(Exception exception)
    {
        if (exception is HttpRequestException httpEx)
        {
            return httpEx.StatusCode switch
            {
                HttpStatusCode.RequestTimeout => true,
                HttpStatusCode.TooManyRequests => true,
                HttpStatusCode.InternalServerError => true,
                HttpStatusCode.BadGateway => true,
                HttpStatusCode.ServiceUnavailable => true,
                HttpStatusCode.GatewayTimeout => true,
                _ => false
            };
        }

        if (exception is TaskCanceledException or OperationCanceledException)
            return true;

        if (exception is AzuraCastException azEx)
        {
            return azEx.ErrorCode == ErrorCode.ServiceUnavailable 
                   || azEx.ErrorCode == ErrorCode.RequestTimeout
                   || azEx.ErrorCode == ErrorCode.TooManyRequests;
        }

        return false;
    }

    public bool ShouldRetry(Exception exception, int attemptNumber)
    {
        // Don't retry after max attempts
        if (attemptNumber >= 3)
            return false;

        // Only retry transient errors
        if (!IsTransientError(exception))
            return false;

        // Don't retry authentication errors
        if (exception is HttpRequestException httpEx && 
            httpEx.StatusCode == HttpStatusCode.Unauthorized)
            return false;

        return true;
    }

    private string GetAzuraCastErrorMessage(AzuraCastException exception)
    {
        return exception.ErrorCode switch
        {
            ErrorCode.Unauthorized => 
                "Authentication failed with AzuraCast. Please verify your API key configuration.",
            ErrorCode.Forbidden => 
                "Access denied. You don't have permission to perform this operation on AzuraCast.",
            ErrorCode.NotFound => 
                "The requested resource was not found on AzuraCast.",
            ErrorCode.BadRequest => 
                $"Invalid request to AzuraCast: {exception.Message}",
            ErrorCode.Conflict => 
                $"Conflict with existing data on AzuraCast: {exception.Message}",
            ErrorCode.RequestTimeout => 
                "Request to AzuraCast timed out. The server may be overloaded.",
            ErrorCode.TooManyRequests => 
                "Too many requests to AzuraCast. Please wait a moment and try again.",
            ErrorCode.ServiceUnavailable => 
                "AzuraCast service is currently unavailable. Please try again later.",
            _ => $"AzuraCast error: {exception.Message}"
        };
    }

    private string GetHttpErrorMessage(HttpRequestException exception)
    {
        if (exception.StatusCode == null)
        {
            // Network error (no response from server)
            return "Unable to connect to AzuraCast. Please check your network connection and ensure AzuraCast is running.";
        }

        return exception.StatusCode switch
        {
            HttpStatusCode.Unauthorized => 
                "Authentication failed with AzuraCast. Please verify your API key configuration.",
            HttpStatusCode.Forbidden => 
                "Access denied by AzuraCast. Please check your permissions.",
            HttpStatusCode.NotFound => 
                "The requested resource was not found on AzuraCast.",
            HttpStatusCode.BadRequest => 
                $"Invalid request to AzuraCast: {exception.Message}",
            HttpStatusCode.RequestTimeout => 
                "Request to AzuraCast timed out.",
            HttpStatusCode.TooManyRequests => 
                "Rate limit exceeded. Please wait before making more requests to AzuraCast.",
            HttpStatusCode.InternalServerError => 
                "AzuraCast encountered an internal error. Please contact the administrator.",
            HttpStatusCode.BadGateway => 
                "Bad gateway error when connecting to AzuraCast.",
            HttpStatusCode.ServiceUnavailable => 
                "AzuraCast service is temporarily unavailable.",
            HttpStatusCode.GatewayTimeout => 
                "Gateway timeout when connecting to AzuraCast.",
            _ => $"HTTP error {(int)exception.StatusCode}: {exception.Message}"
        };
    }
}
