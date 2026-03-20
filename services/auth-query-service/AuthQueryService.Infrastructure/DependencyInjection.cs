using AuthQueryService.Application.Abstractions;
using AuthQueryService.Application.Abstractions.Messaging.Dispatcher;
using AuthQueryService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthQueryService.Domain.Interfaces;
using AuthQueryService.Infrastructure.ExternalServices;
using AuthQueryService.Infrastructure.Messaging;
using AuthQueryService.Infrastructure.Messaging.EventHandlers;
using AuthQueryService.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace AuthQueryService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure MongoDB GUID serialization
        try
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }
        catch (BsonSerializationException)
        {
            // Already registered, ignore
        }

        // MongoDB for Read side

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            // Read from environment variables first (Docker/Kubernetes), then from config
            var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
                                ?? configuration.GetConnectionString("MongoDb")?.Replace("${MONGODB_CONNECTION_STRING}", "")?.Trim()
                                ?? configuration["MONGODB_CONNECTION_STRING"]
                                ?? "mongodb://mongodb:27017";
            
            // Remove placeholder syntax if present
            if (connectionString.Contains("${"))
            {
                connectionString = "mongodb://mongodb:27017"; // Default to service name in Docker
            }
            
            var databaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE")
                            ?? configuration["Mongo:Database"]?.Replace("${MONGODB_DATABASE}", "")?.Trim()
                            ?? configuration["MONGODB_DATABASE"]
                            ?? "auth_query";
            
            // Remove placeholder syntax if present
            if (databaseName.Contains("${"))
            {
                databaseName = "auth_query";
            }
            
            var client = new MongoClient(connectionString);
            return client.GetDatabase(databaseName);
        });

        // MongoDB Repositories (directly connect to MongoDB, no DAO layer)
        services.AddScoped<IUserReadRepository, UserReadRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserActivityLogRepository, UserActivityLogRepository>();
        services.AddScoped<IFavouriteReadRepository, FavouriteReadRepository>();

        // Spotify API Client (HttpClient managed by IHttpClientFactory)
        services.AddHttpClient<AuthQueryService.Application.Abstractions.ISpotifyApiClient, SpotifyApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Query dispatcher (handlers are auto-registered in Application layer via Scrutor)
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // ============================================
        // Event Handlers (Clean Architecture)
        // ============================================
        
        // User Data Sync Handlers
        services.AddScoped<IUserEventHandler, Messaging.EventHandlers.Handlers.UserCreatedEventHandler>();
        services.AddScoped<IUserEventHandler, Messaging.EventHandlers.Handlers.UserUpdatedEventHandler>();
        services.AddScoped<IUserEventHandler, Messaging.EventHandlers.Handlers.UserProfileUpdatedEventHandler>();
        services.AddScoped<IUserEventHandler, Messaging.EventHandlers.Handlers.UserBannedEventHandler>();
        services.AddScoped<IUserEventHandler, Messaging.EventHandlers.Handlers.UserUnbannedEventHandler>();
        services.AddScoped<IUserEventHandler, Messaging.EventHandlers.Handlers.UserDeactivatedEventHandler>();
        services.AddScoped<IUserEventHandler, Messaging.EventHandlers.Handlers.UserDeletedEventHandler>();
        
        // Activity/Security Event Handlers
        services.AddScoped<IActivityEventHandler, Messaging.EventHandlers.ActivityHandlers.LoginSuccessfulHandler>();
        services.AddScoped<IActivityEventHandler, Messaging.EventHandlers.ActivityHandlers.LoginFailedHandler>();
        services.AddScoped<IActivityEventHandler, Messaging.EventHandlers.ActivityHandlers.LoginActivityHandler>();
        services.AddScoped<IActivityEventHandler, Messaging.EventHandlers.ActivityHandlers.GoogleLoginSuccessfulHandler>();
        services.AddScoped<IActivityEventHandler, Messaging.EventHandlers.ActivityHandlers.GoogleLoginFailedHandler>();
        services.AddScoped<IActivityEventHandler, Messaging.EventHandlers.ActivityHandlers.RegistrationFailedHandler>();
        services.AddScoped<IActivityEventHandler, Messaging.EventHandlers.ActivityHandlers.TokenRefreshedHandler>();
        
        // Event Handler Factory
        services.AddSingleton<IEventHandlerFactory>(sp =>
        {
            var handlers = sp.GetServices<IUserEventHandler>();
            return new Messaging.EventHandlers.EventHandlerFactory(sp, handlers);
        });

        // ============================================
        // Background Projection Services (Clean & Refactored)
        // ============================================
        services.AddHostedService<RabbitMqUserProjectionService>();
        services.AddHostedService<RabbitMqRoleProjectionService>();
        services.AddHostedService<RabbitMqActivityProjectionService>();
        
        
        // Register RabbitMQ publisher
        services.AddScoped<IMessageBusPublisher, RabbitMqPublisher>();

        return services;
    }
}