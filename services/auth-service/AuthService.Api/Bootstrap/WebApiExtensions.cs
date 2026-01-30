using AuthService.Api.Models.Responses;
using AuthService.Application.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Bootstrap;

/// <summary>
/// Extension methods for Web API configuration
/// Handles controllers, JSON serialization, model validation, and routing
/// </summary>
public static class WebApiExtensions
{
    public static WebApplicationBuilder AddWebApi(this WebApplicationBuilder builder)
    {
        // Add controllers with JSON options
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = 
                    System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

        // Handle model validation errors (400) with ApiResponse format
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var firstError = context.ModelState
                    .Where(kvp => kvp.Value?.Errors.Count > 0)
                    .Select(kvp => $"{kvp.Key}: {kvp.Value!.Errors.First().ErrorMessage}")
                    .FirstOrDefault() ?? "Missing/Invalid input";
                
                var body = ApiResponse<string>.Error(ApiStatusCode.HB40001, firstError);
                return new BadRequestObjectResult(body);
            };
        });

        // Configure routing
        builder.Services.AddRouting(options => options.LowercaseUrls = true);
        
        // Add API explorer for Swagger
        builder.Services.AddEndpointsApiExplorer();

        return builder;
    }
}
