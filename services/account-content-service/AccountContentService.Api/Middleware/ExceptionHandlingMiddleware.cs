using AccountContentService.Api.Common;
using AccountContentService.Application.Exceptions;
using System.Net;
using System.Text.Json;

namespace AccountContentService.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, userMessage, logLevel) = exception switch
        {
            // 409 Conflict — user tried to buy a subscription they already have
            SubscriptionAlreadyExistsException ex => (
                HttpStatusCode.Conflict,
                ex.Message,
                LogLevelLevel.Warning
            ),

            // 404 Not Found — subscription plan doesn't exist or is inactive
            SubscriptionPlanNotFoundException ex => (
                HttpStatusCode.NotFound,
                ex.Message,
                LogLevelLevel.Warning
            ),

            // 400 Bad Request — validation errors
            ValidationException ex => (
                HttpStatusCode.BadRequest,
                ex.Message,
                LogLevelLevel.Warning
            ),

            // 404 Not Found — generic not found
            NotFoundException ex => (
                HttpStatusCode.NotFound,
                ex.Message,
                LogLevelLevel.Warning
            ),

            // Let other known business exceptions bubble with their own message
            Application.Exceptions.ApplicationException ex => (
                HttpStatusCode.BadRequest,
                ex.Message,
                LogLevelLevel.Warning
            ),

            // Unknown / infrastructure errors — hide details from user, log full stack
            _ => (
                HttpStatusCode.InternalServerError,
                "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.",
                LogLevelLevel.Error
            )
        };

        if (logLevel == LogLevelLevel.Error)
        {
            _logger.LogError(exception, "Unhandled exception: {Type} — {Message}", exception.GetType().Name, exception.Message);
        }
        else
        {
            _logger.LogWarning("Handled business exception: {Type} — {Message}", exception.GetType().Name, exception.Message);
        }

        var response = ApiResponse<string>.Fail(userMessage, userMessage);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

// Alias to avoid conflict with Microsoft.Extensions.Logging.LogLevel
internal enum LogLevelLevel { Warning, Error }
