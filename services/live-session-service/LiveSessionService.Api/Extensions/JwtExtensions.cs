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
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var secretKey  = jwtSection["Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured in appsettings.");
        var issuer   = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];

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
