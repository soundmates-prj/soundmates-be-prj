namespace AiService.Api.Extensions;

public static class CorsExtensions
{
    public static WebApplicationBuilder AddCorsPolicy(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
        });

        return builder;
    }
}

