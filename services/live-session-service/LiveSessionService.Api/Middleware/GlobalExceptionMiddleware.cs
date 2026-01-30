using LiveSessionService.Application.Enums;
using LiveSessionService.Api.Models.Responses;
using System.Net;
using System.Text.Json;

namespace LiveSessionService.Api.Middleware;

/// <summary>
/// Global exception handler middleware
/// Catches unhandled exceptions và convert thành API response chu?n
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
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = ApiResponse<object>.FailureResponse(
            message: _env.IsDevelopment() 
                ? $"Internal server error: {exception.Message}" 
                : "An internal server error occurred",
            errorCode: (int)ErrorCode.InternalServerError
        );

        // Trong Development, có th? include stack trace
        if (_env.IsDevelopment())
        {
            var detailedResponse = new
            {
                response.Success,
                response.Message,
                response.ErrorCode,
                ExceptionType = exception.GetType().Name,
                StackTrace = exception.StackTrace
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
/// Extension method ?? register middleware d? h?n
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
