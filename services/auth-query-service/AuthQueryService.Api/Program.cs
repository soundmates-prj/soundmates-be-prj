using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Application.Enums;
using AuthQueryService.Infrastructure.Middlewares;
using AuthQueryService.Application;
using AuthQueryService.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
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

// Support environment variables for JWT configuration
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key not configured. Set JWT_KEY environment variable or configure in appsettings.json");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "SoundmatesAuthService";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["Jwt:Audience"]
    ?? "SoundmatesUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimTypes.NameIdentifier,
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
                    ctx.Token = auth.Substring("Bearer ".Length).Trim();
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
            var payload = JsonSerializer.Serialize(
                ApiResponse<string>.Error(ApiStatusCode.HB40101, "Token missing/Invalid"));
            await context.Response.WriteAsync(payload);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var payload = JsonSerializer.Serialize(
                ApiResponse<string>.Error(ApiStatusCode.HB40301, "Permission Denied"));
            await context.Response.WriteAsync(payload);
        }
    };
});

builder.Services.AddAuthorization();

// ── Spotify configuration ──────────────────────────────────────────────
// Reads SPOTIFY_CLIENT_ID / SPOTIFY_CLIENT_SECRET from environment variables
// and exposes them as Spotify:ClientId / Spotify:ClientSecret in IConfiguration
// so that SpotifyApiClient (registered via AddHttpClient) can consume them.
var spotifyClientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID")
    ?? builder.Configuration["Spotify:ClientId"];
var spotifyClientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET")
    ?? builder.Configuration["Spotify:ClientSecret"];

if (!string.IsNullOrWhiteSpace(spotifyClientId))
    builder.Configuration["Spotify:ClientId"] = spotifyClientId;
if (!string.IsNullOrWhiteSpace(spotifyClientSecret))
    builder.Configuration["Spotify:ClientSecret"] = spotifyClientSecret;

builder.Services.AddAuthApplication();
builder.Services.AddAuthInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Swagger + JWT security
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "QueryService API", Version = "v1" });

    // Enable XML comments for standard API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: Bearer {token}"
    });

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (response.StatusCode == StatusCodes.Status404NotFound &&
        !response.HasStarted)
    {
        response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(
            ApiResponse<string>.Error(ApiStatusCode.HB40401, "Resource not found"));
        await response.WriteAsync(payload);
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
