using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Application.Enums;
using AuthQueryService.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Net;
using System.Text.Json;

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
            _logger.LogError(ex, "Unhandled exception occurred");

            var response = context.Response;
            response.ContentType = "application/json";

            ApiResponse<object> apiResponse;
            int statusCode = (int)HttpStatusCode.InternalServerError;

            switch (ex)
            {
                case AuthException authEx:
                    statusCode = authEx.ErrorCode == AuthErrorCode.UserAlreadyExists
                        ? StatusCodes.Status409Conflict
                        : StatusCodes.Status400BadRequest;
                    apiResponse = ApiResponse<object>.FailureResponse(authEx.Message, (int)authEx.ErrorCode);
                    break;

                case DbUpdateException dbEx when dbEx.InnerException is PostgresException pgEx:
                    if (pgEx.SqlState == "23505") // duplicate key
                    {
                        statusCode = StatusCodes.Status409Conflict;
                        apiResponse = ApiResponse<object>.FailureResponse("Duplicate entry detected", 409);
                    }
                    else
                    {
                        apiResponse = ApiResponse<object>.FailureResponse(pgEx.Message, 500);
                    }
                    break;

                default:
                    apiResponse = ApiResponse<object>.FailureResponse("An unexpected error occurred", 500);
                    break;
            }

            response.StatusCode = statusCode;
            var json = JsonSerializer.Serialize(apiResponse);
            await response.WriteAsync(json);
        }
    }
}

