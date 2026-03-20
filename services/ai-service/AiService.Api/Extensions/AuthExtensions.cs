using AiService.Api.Models.Responses;
using AiService.Application;
using AiService.Application.Enums;
using AiService.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace AiService.Api.Extensions;

public static class AuthExtensions
{
    public static WebApplicationBuilder AddAuth(this WebApplicationBuilder builder)
    {
        builder.Services.AddAiInfrastructure(builder.Configuration);
        builder.Services.AddAiApplication();

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwt = builder.Configuration.GetSection("Jwt");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var auth = ctx.Request.Headers["Authorization"].ToString();
                        if (!string.IsNullOrWhiteSpace(auth))
                        {
                            if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                                ctx.Token = auth["Bearer ".Length..].Trim();
                            else
                                ctx.Token = auth.Trim();
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(
                            ApiResponse<string>.Error(ApiStatusCode.HB40101, "Token missing/Invalid")));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(
                            ApiResponse<string>.Error(ApiStatusCode.HB40301, "Permission Denied")));
                    }
                };
            });

        builder.Services.AddAuthorization();
        return builder;
    }
}

