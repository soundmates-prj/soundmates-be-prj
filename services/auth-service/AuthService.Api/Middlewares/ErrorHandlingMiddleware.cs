using AuthService.Api.Models.Responses;
using AuthService.Application.Enums;
using AuthService.Application.Exceptions;
using System.Net;
using System.Text.Json;

namespace AuthService.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var statusCode = HttpStatusCode.InternalServerError;
        var apiStatusCode = ApiStatusCode.HB50001;
        var message = "An unexpected error occurred in SoundMates System";

        switch (exception)
        {
            case AuthException authEx:
                (statusCode, apiStatusCode) = authEx.ErrorCode switch
                {
                    AuthErrorCode.InvalidCredentials => (HttpStatusCode.BadRequest, ApiStatusCode.HB40001),
                    AuthErrorCode.UserAlreadyExists => (HttpStatusCode.Conflict, ApiStatusCode.HB40901),
                    AuthErrorCode.RegistrationFailed => (HttpStatusCode.BadRequest, ApiStatusCode.HB40001),
                    AuthErrorCode.EmailSendFailed => (HttpStatusCode.ServiceUnavailable, ApiStatusCode.HB50001),
                    AuthErrorCode.AccountLocked => (HttpStatusCode.Forbidden, ApiStatusCode.HB40302),
                    AuthErrorCode.AccountBanned => (HttpStatusCode.Forbidden, ApiStatusCode.HB40302),
                    _ => (HttpStatusCode.BadRequest, ApiStatusCode.HB40001)
                };
                message = authEx.Message;
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                apiStatusCode = ApiStatusCode.HB40101;
                message = "Unauthorized access";
                break;

            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                apiStatusCode = ApiStatusCode.HB40401;
                message = exception.Message;
                break;

            case ArgumentException:
                statusCode = HttpStatusCode.BadRequest;
                apiStatusCode = ApiStatusCode.HB40001;
                message = exception.Message;
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<string>.Error(apiStatusCode, message);
        var jsonResponse = JsonSerializer.Serialize(response);

        return context.Response.WriteAsync(jsonResponse);
    }
}
