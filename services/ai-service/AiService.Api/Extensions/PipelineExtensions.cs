using AiService.Api.Middleware;
using AiService.Api.Models.Responses;
using AiService.Application.Enums;
using System.Text.Json;

namespace AiService.Api.Extensions;

public static class PipelineExtensions
{
    public static WebApplication UseHttpPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (app.Environment.IsProduction())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseMiddleware<ErrorHandlingMiddleware>();

        app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;
            if (response.StatusCode == StatusCodes.Status404NotFound && !response.HasStarted)
            {
                response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(
                    ApiResponse<string>.Error(ApiStatusCode.HB40401, "Resource not found"));
                await response.WriteAsync(payload);
            }
        });

        app.UseCors("AllowAll");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}

