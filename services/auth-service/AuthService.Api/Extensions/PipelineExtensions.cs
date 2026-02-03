using AuthService.Api.Middleware;
using AuthService.Api.Models.Responses;
using AuthService.Application.Enums;
using System.Text.Json;

namespace AuthService.Api.Extensions;

/// <summary>
/// Extension methods for HTTP request pipeline configuration
/// Configures middleware, error handling, and routing
/// </summary>
public static class PipelineExtensions
{
    public static WebApplication UseHttpPipeline(this WebApplication app)
    {
        // Swagger (Development only)
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // HTTPS redirect (Production only)
        if (app.Environment.IsProduction())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        // Global error handler
        app.UseMiddleware<ErrorHandlingMiddleware>();

        // Handle 404 errors with ApiResponse format
        app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;
            if (response.StatusCode == StatusCodes.Status404NotFound && !response.HasStarted)
            {
                response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(
                    ApiResponse<string>.Error(
                        ApiStatusCode.HB40401, 
                        "Resource not found"));
                await response.WriteAsync(payload);
            }
        });

        // Request pipeline
        app.UseCors("AllowAll");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
