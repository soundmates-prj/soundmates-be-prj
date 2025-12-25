using AuthService.Application.DTOs.Response;
using AuthService.Application.Enums;
using AuthService.Infrastructure;
using AuthService.Application;
using AuthService.Infrastructure.Messaging;
using AuthService.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DotNetEnv;

// Load .env file from solution root (parent directory)
try
{
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
    if (File.Exists(envPath))
    {
        Env.Load(envPath);
    }
    else
    {
        // Try current directory
        Env.Load();
    }
}
catch (Exception)
{
    // .env file not found, will use appsettings.json or environment variables
}

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
// This will override appsettings.json values if environment variables are set
foreach (System.Collections.DictionaryEntry envVar in Environment.GetEnvironmentVariables())
{
    var key = envVar.Key?.ToString();
    var value = envVar.Value?.ToString();
    
    if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        continue;

    // Map PostgreSQL environment variables to ConnectionStrings:DefaultConnection
    if (key.StartsWith("POSTGRES_"))
    {
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT");
        var database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
        var username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME");
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
        
        if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(port))
        {
            var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Disable;Trust Server Certificate=True;";
            builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
        }
    }
    
    // Map RabbitMQ environment variables
    else if (key == "RABBITMQ_HOST")
        builder.Configuration["RabbitMq:HostName"] = value;
    else if (key == "RABBITMQ_PORT")
        builder.Configuration["RabbitMq:Port"] = value;
    else if (key == "RABBITMQ_USERNAME")
        builder.Configuration["RabbitMq:UserName"] = value;
    else if (key == "RABBITMQ_PASSWORD")
        builder.Configuration["RabbitMq:Password"] = value;
    else if (key == "RABBITMQ_VIRTUALHOST")
        builder.Configuration["RabbitMq:VirtualHost"] = value;
    
    // Map JWT environment variables
    else if (key == "JWT_KEY")
        builder.Configuration["Jwt:Key"] = value;
    else if (key == "JWT_ISSUER")
        builder.Configuration["Jwt:Issuer"] = value;
    else if (key == "JWT_AUDIENCE")
        builder.Configuration["Jwt:Audience"] = value;
    else if (key == "JWT_EXPIRES_IN_HOURS")
        builder.Configuration["Jwt:ExpiresInHours"] = value;
    
    // Map Google OAuth environment variables
    else if (key == "GOOGLE_CLIENT_ID")
        builder.Configuration["GoogleOAuth:ClientId"] = value;
    else if (key == "GOOGLE_CLIENT_SECRET")
        builder.Configuration["GoogleOAuth:ClientSecret"] = value;
    
    // Map Email environment variables
    else if (key == "EMAIL_HOST")
        builder.Configuration["EmailSettings:Host"] = value;
    else if (key == "EMAIL_PORT")
        builder.Configuration["EmailSettings:Port"] = value;
    else if (key == "EMAIL_FROM")
        builder.Configuration["EmailSettings:From"] = value;
    else if (key == "EMAIL_USERNAME")
        builder.Configuration["EmailSettings:Username"] = value;
    else if (key == "EMAIL_PASSWORD")
        builder.Configuration["EmailSettings:Password"] = value;
    
    // Map App Settings environment variables
    else if (key == "FRONTEND_URL")
        builder.Configuration["AppSettings:FrontendUrl"] = value;
}

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Handling ERROR 400 (model validation) to ApiResponse
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

builder.Services.AddEndpointsApiExplorer();
// Lowercase URLs for apis
builder.Services.AddRouting(options => options.LowercaseUrls = true);
// Add RabbitMQ publisher (single registration)
builder.Services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();

// Swagger + JWT security
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthService API", Version = "v1" });

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
            new string[] {}
        }
    });
});


// Application and Infrastructure layers
builder.Services.AddAuthInfrastructure(builder.Configuration);
builder.Services.AddAuthApplication();

// Cors setup
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// JWT setup
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
        RoleClaimType = ClaimTypes.Role
    };

    // Customize authentication failure response
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

var app = builder.Build();

// Apply database migrations automatically on startup
// This MUST complete successfully before the app starts
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthService.Infrastructure.Data.AuthDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting database migration...");

    const int maxRetries = 5;
    const int baseDelaySeconds = 2;
    var retryCount = 0;
    var success = false;

    while (retryCount < maxRetries && !success)
    {
        try
        {
            if (retryCount > 0)
            {
                var delaySeconds = baseDelaySeconds * (int)Math.Pow(2, retryCount - 1);
                logger.LogInformation("Retry attempt {RetryCount}/{MaxRetries} after {DelaySeconds} seconds...", 
                    retryCount, maxRetries, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), CancellationToken.None);
            }

            // Ensure database is created with retry logic
            var canConnect = false;
            try
            {
                canConnect = await dbContext.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning("Database connection attempt failed: {Message}. Will retry...", ex.Message);
                retryCount++;
                continue;
            }

            logger.LogInformation("Database connection check: {CanConnect}", canConnect);

            if (!canConnect)
            {
                logger.LogWarning("Cannot connect to database. Attempting to create database...");
                try
                {
                    await dbContext.Database.EnsureCreatedAsync();
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Failed to create database: {Message}. Will retry...", ex.Message);
                    retryCount++;
                    continue;
                }
            }

            // Get pending migrations before applying
            var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
            var appliedMigrations = dbContext.Database.GetAppliedMigrations().ToList();
            
            logger.LogInformation("Applied migrations: {Count} - {Migrations}", 
                appliedMigrations.Count, string.Join(", ", appliedMigrations));
            logger.LogInformation("Pending migrations: {Count} - {Migrations}", 
                pendingMigrations.Count, string.Join(", ", pendingMigrations));

            // Always call MigrateAsync() - it's safe to call even if no migrations are pending
            // This ensures the database schema is always up to date
            logger.LogInformation("Applying migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migration completed successfully.");

            // Verify migration was applied
            var remainingPending = dbContext.Database.GetPendingMigrations().ToList();
            if (remainingPending.Any())
            {
                logger.LogWarning("Warning: Some migrations may not have been applied: {Migrations}", 
                    string.Join(", ", remainingPending));
            }
            else
            {
                logger.LogInformation("All migrations have been applied successfully.");
            }

            success = true;
        }
        catch (Exception ex)
        {
            retryCount++;
            if (retryCount >= maxRetries)
            {
                logger.LogCritical(ex, "CRITICAL: Database migration failed after {MaxRetries} attempts. Application will not start.", maxRetries);
                logger.LogCritical("Error details: {Message}\n{StackTrace}", ex.Message, ex.StackTrace);
                throw;
            }
            else
            {
                logger.LogWarning(ex, "Migration attempt {RetryCount} failed: {Message}. Will retry...", retryCount, ex.Message);
            }
        }
    }

    if (!success)
    {
        logger.LogCritical("CRITICAL: Failed to apply database migrations after {MaxRetries} attempts.", maxRetries);
        throw new InvalidOperationException("Database migration failed. Please check the database connection and try again.");
    }

    // Seed default roles if they don't exist
    try
    {
        await SeedDefaultRolesAsync(scope, logger);

        // Wait a bit for roles to be published and synced to MongoDB
        // This ensures roles are available in query service before accepting requests
        logger.LogInformation("Waiting for roles to be synced to query service...");
        await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);
        logger.LogInformation("Role seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during role seeding. Application will continue, but roles may not be available.");
        // Don't throw - role seeding failure shouldn't prevent the app from starting
    }
}

// Helper method to seed default roles
static async Task SeedDefaultRolesAsync(AsyncServiceScope scope, ILogger logger)
{
    try
    {
        var roleRepository = scope.ServiceProvider.GetRequiredService<AuthService.Domain.Interfaces.IRoleRepository>();
        var commandDispatcher = scope.ServiceProvider.GetRequiredService<AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces.ICommandDispatcher>();

        var defaultRoles = new[] { "USER", "HOST", "ADMIN" };

        foreach (var roleName in defaultRoles)
        {
            var existingRole = await roleRepository.GetByNameAsync(roleName);
            if (existingRole == null)
            {
                logger.LogInformation("Creating default role: {RoleName}", roleName);
                var createRoleCmd = new AuthService.Application.Services.Role.Commands.CreateRoleCommand { Name = roleName };
                var result = await commandDispatcher.Send<AuthService.Application.Services.Role.Commands.CreateRoleCommand, Guid>(createRoleCmd, CancellationToken.None);

                if (result.Success)
                {
                    logger.LogInformation("Successfully created role: {RoleName}", roleName);
                }
                else
                {
                    logger.LogWarning("Failed to create role {RoleName}: {Error}", roleName, result.Message);
                }
            }
            else
            {
                logger.LogDebug("Role {RoleName} already exists", roleName);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error seeding default roles. Continuing anyway...");
        // Don't throw - roles might already exist
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only redirect to HTTPS in Production
if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Global error handler (maps exceptions to ApiResponse with proper HTTP status)
app.UseMiddleware<ErrorHandlingMiddleware>();

// For non-exception 404s (no matching endpoint), return ApiResponse
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

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
