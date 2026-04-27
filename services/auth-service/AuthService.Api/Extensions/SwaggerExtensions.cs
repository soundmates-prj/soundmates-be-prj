using Microsoft.OpenApi.Models;
using System.Reflection;

namespace AuthService.Api.Extensions;

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
                Title = "AuthService API", 
                Version = "v1",
                Description = "SoundMates - Authentication and Authorization Service API"
            });

            // Load XML comments để hiển thị summary/remarks từ Controllers/Models trong Swagger
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

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
