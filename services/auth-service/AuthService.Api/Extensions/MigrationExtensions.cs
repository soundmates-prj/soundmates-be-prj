using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Extensions;

/// <summary>
/// Extension methods for database migration and seeding
/// Handles automatic database migrations with retry logic and default data seeding
/// </summary>
public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
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
                    logger.LogInformation(
                        "Retry attempt {RetryCount}/{MaxRetries} after {DelaySeconds} seconds...", 
                        retryCount, maxRetries, delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), CancellationToken.None);
                }

                // Check database connection
                var canConnect = false;
                try
                {
                    canConnect = await dbContext.Database.CanConnectAsync();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        "Database connection attempt failed: {Message}. Will retry...", 
                        ex.Message);
                    retryCount++;
                    continue;
                }

                logger.LogInformation("Database connection check: {CanConnect}", canConnect);

                // Get pending migrations
                var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
                var appliedMigrations = dbContext.Database.GetAppliedMigrations().ToList();
                
                logger.LogInformation(
                    "Applied migrations: {Count} - {Migrations}", 
                    appliedMigrations.Count, 
                    string.Join(", ", appliedMigrations));
                logger.LogInformation(
                    "Pending migrations: {Count} - {Migrations}", 
                    pendingMigrations.Count, 
                    string.Join(", ", pendingMigrations));

                // Apply migrations
                logger.LogInformation("Applying migrations...");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migration completed successfully.");

                // Verify migration
                var remainingPending = dbContext.Database.GetPendingMigrations().ToList();
                if (remainingPending.Any())
                {
                    logger.LogWarning(
                        "Warning: Some migrations may not have been applied: {Migrations}", 
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
                    logger.LogCritical(
                        ex, 
                        "CRITICAL: Database migration failed after {MaxRetries} attempts. Application will not start.", 
                        maxRetries);
                    logger.LogCritical("Error details: {Message}\n{StackTrace}", ex.Message, ex.StackTrace);
                    throw;
                }
                else
                {
                    logger.LogWarning(
                        ex, 
                        "Migration attempt {RetryCount} failed: {Message}. Will retry...", 
                        retryCount, ex.Message);
                }
            }
        }

        if (!success)
        {
            logger.LogCritical(
                "CRITICAL: Failed to apply database migrations after {MaxRetries} attempts.", 
                maxRetries);
            throw new InvalidOperationException(
                "Database migration failed. Please check the database connection and try again.");
        }
    }

    public static async Task SeedDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            await SeedDefaultRolesAsync(scope, logger);

            // Wait for roles to be synced to query service
            logger.LogInformation("Waiting for roles to be synced to query service...");
            await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);
            logger.LogInformation("Role seeding completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex, 
                "Error during role seeding. Application will continue, but roles may not be available.");
            // Don't throw - seeding failure shouldn't prevent app from starting
        }
    }

    private static async Task SeedDefaultRolesAsync(AsyncServiceScope scope, ILogger logger)
    {
        try
        {
            var roleRepository = scope.ServiceProvider
                .GetRequiredService<AuthService.Domain.Interfaces.IRoleRepository>();
            var commandDispatcher = scope.ServiceProvider
                .GetRequiredService<AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces.ICommandDispatcher>();

            // Seed core roles - GUEST is not stored in DB
            var defaultRoles = new[] { "MEMBER", "HOST", "STAFF", "ADMIN" };

            foreach (var roleName in defaultRoles)
            {
                var existingRole = await roleRepository.GetByNameAsync(roleName);
                if (existingRole == null)
                {
                    logger.LogInformation("Creating default role: {RoleName}", roleName);
                    
                    var createRoleCmd = new AuthService.Application.Features.Role.Commands.CreateRoleCommand 
                    { 
                        Name = roleName 
                    };
                    
                    var result = await commandDispatcher.Send<
                        AuthService.Application.Features.Role.Commands.CreateRoleCommand, 
                        Guid>(createRoleCmd, CancellationToken.None);

                    if (result.IsSuccess && result.Data != Guid.Empty)
                    {
                        logger.LogInformation(
                            "Successfully created role: {RoleName} with ID: {RoleId}", 
                            roleName, result.Data);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Failed to create role {RoleName}: {Error}", 
                            roleName, result.ErrorMessage ?? "Unknown error");
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
        }
    }
}
