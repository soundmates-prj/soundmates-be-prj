using AiService.Api.Models.Responses;
using AiService.Application.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AiService.Api.Extensions;

public static class WebApiExtensions
{
    public static WebApplicationBuilder AddWebApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler =
                    System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

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

        builder.Services.AddRouting(options => options.LowercaseUrls = true);
        builder.Services.AddEndpointsApiExplorer();

        return builder;
    }
}

