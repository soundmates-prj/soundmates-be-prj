using Microsoft.OpenApi.Models;

namespace LiveSessionService.Api.Extensions;

/// <summary>
/// Extension methods for Swagger/OpenAPI configuration
/// Configures Swagger with JWT Bearer authentication
/// </summary>
public static class SwaggerExtensions
{
    public static WebApplicationBuilder AddSwaggerWithJwt(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LiveSessionService API",
                Version = "v1",
                Description = "Live Session Management Service API with AzuraCast Integration"
            });

            // Add JWT security definition
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token. Example: Bearer {token}"
            });

            // Add security requirement
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return builder;
    }
}
