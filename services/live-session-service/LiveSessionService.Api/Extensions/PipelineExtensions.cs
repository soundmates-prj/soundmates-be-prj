using LiveSessionService.Api.Hubs;
using LiveSessionService.Api.Middleware;
using LiveSessionService.Api.Models.Responses;
using System.Text.Json;

namespace LiveSessionService.Api.Extensions;

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
        app.UseGlobalExceptionHandler();

        // Handle 404 errors with ApiResponse format
        app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;
            if (response.StatusCode == StatusCodes.Status404NotFound && !response.HasStarted)
            {
                response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(
                    ApiResponse<string>.FailureResponse(
                        "Resource not found",
                        404));
                await response.WriteAsync(payload);
            }
        });

        // Request pipeline
        app.UseCors("AllowAll");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<NowPlayingHub>("/hubs/now-playing");
        app.MapHub<LiveSessionHub>("/hubs/live-session");

        return app;
    }
}
