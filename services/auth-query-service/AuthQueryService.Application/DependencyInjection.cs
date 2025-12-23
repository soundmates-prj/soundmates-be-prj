using System.Reflection;
using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.Abstractions.Messaging.Dispatcher;
using AuthQueryService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AuthQueryService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        // Query Dispatcher (registered in Infrastructure layer with Query Handlers)
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();
        // This layer is for Application-specific services only
        
        return services;
    }
}