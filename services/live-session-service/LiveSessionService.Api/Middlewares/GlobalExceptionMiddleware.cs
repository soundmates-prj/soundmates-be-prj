    using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Exceptions;
using LiveSessionService.Api.Models.Responses;
using System.Net;
using System.Text.Json;

namespace LiveSessionService.Api.Middleware;

/// <summary>
/// Global exception handler middleware
/// Catches unhandled exceptions và convert thành API response chuẩn
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var errorCode = ErrorCode.InternalServerError;
        var message = "An unexpected error occurred";
        var statusCode = HttpStatusCode.InternalServerError;

        // Map exception types to ErrorCode
        switch (exception)
        {
            case AzuraCastException azEx:
                errorCode = azEx.ErrorCode;
                message = azEx.Message;
                statusCode = (HttpStatusCode)errorCode;
                break;

            case InvalidOperationException:
                errorCode = ErrorCode.BadRequest;
                message = exception.Message;
                statusCode = HttpStatusCode.BadRequest;
                break;

            case KeyNotFoundException:
                errorCode = ErrorCode.NotFound;
                message = exception.Message;
                statusCode = HttpStatusCode.NotFound;
                break;

            case UnauthorizedAccessException:
                errorCode = ErrorCode.Unauthorized;
                message = "Unauthorized access";
                statusCode = HttpStatusCode.Unauthorized;
                break;

            case ArgumentException:
                errorCode = ErrorCode.BadRequest;
                message = exception.Message;
                statusCode = HttpStatusCode.BadRequest;
                break;

            default:
                errorCode = ErrorCode.InternalServerError;
                message = _env.IsDevelopment() 
                    ? $"Internal server error: {exception.Message}" 
                    : "An internal server error occurred";
                statusCode = HttpStatusCode.InternalServerError;
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.FailureResponse(message, (int)errorCode);

        // In Development, include exception details
        if (_env.IsDevelopment() && exception is not AzuraCastException)
        {
            var detailedResponse = new
            {
                response.Success,
                response.Message,
                response.ErrorCode,
                ExceptionType = exception.GetType().Name,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message
            };

            var json = JsonSerializer.Serialize(detailedResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            await context.Response.WriteAsync(json);
            return;
        }

        // Production - không expose details
        var productionJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(productionJson);
    }
}

/// <summary>
/// Extension method to register GlobalExceptionMiddleware
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
