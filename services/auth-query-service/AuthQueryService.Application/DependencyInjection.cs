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
        // Register Query Dispatcher
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // Auto-register all query handlers using Scrutor assembly scanning
        // This eliminates manual registration in Infrastructure layer
        services.Scan(scan => scan
            .FromAssemblies(Assembly.GetExecutingAssembly())
            .AddClasses(classes => classes
                .AssignableTo(typeof(IQueryHandler<,>))
                .Where(c => !c.IsAbstract && !c.IsInterface))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}