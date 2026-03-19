using AiService.Api.Models.Responses;
using AiService.Application.Enums;
using System.Net;
using System.Text.Json;

namespace AiService.Api.Middleware;

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
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
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
        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

