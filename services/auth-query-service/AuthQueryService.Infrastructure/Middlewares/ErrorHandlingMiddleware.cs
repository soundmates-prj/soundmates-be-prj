using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Application.Enums;
using AuthQueryService.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net;
using System.Text.Json;

namespace AuthQueryService.Infrastructure.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await WriteErrorAsync(context, ex);
            }
        }

        private static async Task WriteErrorAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            var (httpStatus, apiCode, message) = MapException(ex);
            context.Response.StatusCode = (int)httpStatus;
            var payload = ApiResponse<string>.Error(apiCode, message);
            var json = JsonSerializer.Serialize(payload);
            await context.Response.WriteAsync(json);
        }

        private static (HttpStatusCode, ApiStatusCode, string) MapException(Exception ex)
        {
            // In development it is extremely useful to see the real exception message
            // instead of a generic "Internal server error". For now we always surface
            // ex.Message for unknown exceptions; known cases are still mapped explicitly.
            return ex switch
            {
                ArgumentNullException ane => (HttpStatusCode.BadRequest, ApiStatusCode.HB40001, ane.Message),
                ArgumentException ae => (HttpStatusCode.BadRequest, ApiStatusCode.HB40001, ae.Message),
                KeyNotFoundException kne => (HttpStatusCode.NotFound, ApiStatusCode.HB40401, kne.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ApiStatusCode.HB40101, "Unauthorized"),
                AuthException authEx when authEx.ErrorCode == AuthErrorCode.UserAlreadyExists => 
                    (HttpStatusCode.Conflict, ApiStatusCode.HB40901, authEx.Message),
                AuthException authEx => 
                    (HttpStatusCode.BadRequest, ApiStatusCode.HB40001, authEx.Message),
                DbUpdateException dbEx when dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505" => 
                    (HttpStatusCode.Conflict, ApiStatusCode.HB40901, "Duplicate entry detected"),
                DbUpdateException dbEx when dbEx.InnerException is PostgresException pgEx => 
                    (HttpStatusCode.InternalServerError, ApiStatusCode.HB50001, pgEx.Message),
                _ => (HttpStatusCode.InternalServerError, ApiStatusCode.HB50001, ex.Message)
            };
        }
    }
}

