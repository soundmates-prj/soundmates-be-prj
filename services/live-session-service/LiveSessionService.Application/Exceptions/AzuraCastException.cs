using LiveSessionService.Application.Enums;

namespace LiveSessionService.Application.Exceptions;

/// <summary>
/// Exception thrown when AzuraCast API fails
/// Provides specific error messages for different scenarios
/// </summary>
public class AzuraCastException : Exception
{
    public ErrorCode ErrorCode { get; }
    public string? AzuraCastUrl { get; protected set; }

    public AzuraCastException(string message, ErrorCode errorCode = ErrorCode.ServiceUnavailable)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public AzuraCastException(string message, string azuraCastUrl, ErrorCode errorCode = ErrorCode.ServiceUnavailable)
        : base(message)
    {
        ErrorCode = errorCode;
        AzuraCastUrl = azuraCastUrl;
    }

    public AzuraCastException(string message, Exception innerException, ErrorCode errorCode = ErrorCode.ServiceUnavailable)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception for authentication/authorization failures with AzuraCast
/// </summary>
public class AzuraCastAuthenticationException : AzuraCastException
{
    public AzuraCastAuthenticationException(string baseUrl)
        : base(
            $"Authentication failed with AzuraCast. Please verify your API configuration at {baseUrl}",
            baseUrl,
            ErrorCode.Unauthorized)
    {
    }
}

/// <summary>
/// Exception when AzuraCast returns no stations
/// </summary>
public class AzuraCastNoStationsException : AzuraCastException
{
    public AzuraCastNoStationsException(string baseUrl)
        : base(
            $"No stations found in AzuraCast at {baseUrl}. Please add stations in AzuraCast admin panel first.",
            baseUrl,
            ErrorCode.NotFound)
    {
    }
}

/// <summary>
/// Exception when cannot connect to AzuraCast
/// </summary>
public class AzuraCastConnectionException : AzuraCastException
{
    public AzuraCastConnectionException(string baseUrl, Exception innerException)
        : base(
            $"Failed to connect to AzuraCast at {baseUrl}. Please verify the base URL and ensure AzuraCast is running.",
            innerException,
            ErrorCode.ServiceUnavailable)
    {
        AzuraCastUrl = baseUrl;
    }
}
