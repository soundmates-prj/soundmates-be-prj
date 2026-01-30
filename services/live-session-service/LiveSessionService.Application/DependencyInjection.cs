using Microsoft.Extensions.DependencyInjection;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Services.NowPlaying.Commands.SyncNowPlaying;
using LiveSessionService.Application.Services.NowPlaying.Queries.GetNowPlaying;
using LiveSessionService.Application.Services.NowPlaying.Queries.GetNowPlayingHistory;
using LiveSessionService.Application.Dtos.Response;

namespace LiveSessionService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register Command Handlers
        services.AddScoped<ICommandHandler<SyncNowPlayingCommand, NowPlayingDto>, SyncNowPlayingHandler>();

        // Register Query Handlers
        services.AddScoped<IQueryHandler<GetNowPlayingQuery, NowPlayingDto>, GetNowPlayingHandler>();
        services.AddScoped<IQueryHandler<GetNowPlayingHistoryQuery, List<NowPlayingHistoryItemDto>>, GetNowPlayingHistoryHandler>();

        return services;
    }
}
