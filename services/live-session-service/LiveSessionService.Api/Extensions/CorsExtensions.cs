namespace LiveSessionService.Api.Extensions;

/// <summary>
/// Extension methods for CORS policy configuration
/// </summary>
public static class CorsExtensions
{
    public static WebApplicationBuilder AddCorsPolicy(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
                policy.WithOrigins(
                          "http://localhost:3000",
                          "http://localhost:5173",
                          "http://localhost:5174")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials());
        });

        return builder;
    }
}
