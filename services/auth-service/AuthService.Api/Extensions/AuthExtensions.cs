using AuthService.Api.Models.Responses;
using AuthService.Application;
using AuthService.Application.Enums;
using AuthService.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace AuthService.Api.Extensions;

/// <summary>
/// Extension methods for authentication and authorization configuration
/// Configures JWT Bearer authentication with custom error responses
/// </summary>
public static class AuthExtensions
{
    public static WebApplicationBuilder AddAuth(this WebApplicationBuilder builder)
    {
        // Register Application and Infrastructure layers
        builder.Services.AddAuthInfrastructure(builder.Configuration);
        builder.Services.AddAuthApplication();

        // Configure JWT authentication
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
                RoleClaimType = ClaimTypes.Role
            };

            // Customize authentication events
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var auth = ctx.Request.Headers["Authorization"].ToString();
                    if (!string.IsNullOrWhiteSpace(auth))
                    {
                        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            ctx.Token = auth.Substring("Bearer ".Length).Trim();
                        else
                            ctx.Token = auth.Trim(); // raw token
                    }
                    return Task.CompletedTask;
                },
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    
                    var payload = JsonSerializer.Serialize(
                        ApiResponse<string>.Error(
                            ApiStatusCode.HB40101, 
                            "Token missing/Invalid"));
                    
                    await context.Response.WriteAsync(payload);
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    
                    var payload = JsonSerializer.Serialize(
                        ApiResponse<string>.Error(
                            ApiStatusCode.HB40301, 
                            "Permission Denied"));
                    
                    await context.Response.WriteAsync(payload);
                }
            };
        });

        return builder;
    }
}
