using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LiveSessionService.Api.Extensions;

/// <summary>
/// Configures JWT Bearer authentication to validate tokens issued by Soundmates.Auth service.
/// </summary>
public static class JwtExtensions
{
    public static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder)
    {
        // Read from environment variables (Docker) or config
        var secretKey = Environment.GetEnvironmentVariable("JWT_KEY")
            ?? builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT_KEY is not configured.");

        if (secretKey.StartsWith("${") || secretKey.Contains("<"))
            throw new InvalidOperationException(
                "JWT_KEY is still a placeholder. " +
                "Set JWT_KEY in your .env file or as an environment variable.");

        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? builder.Configuration["Jwt:Issuer"];
        
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? builder.Configuration["Jwt:Audience"];

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

                    ValidateIssuer   = !string.IsNullOrEmpty(issuer),
                    ValidIssuer      = issuer,

                    ValidateAudience = !string.IsNullOrEmpty(audience),
                    ValidAudience    = audience,

                    ValidateLifetime = true,
                    ClockSkew        = TimeSpan.Zero   // no tolerance for expired tokens
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }
}
