using Microsoft.Extensions.DependencyInjection;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.NowPlaying.Commands.SyncNowPlaying;
using LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlaying;
using LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlayingHistory;
using LiveSessionService.Application.Features.Results.NowPlaying;

namespace LiveSessionService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register Command Handlers
        services.AddScoped<ICommandHandler<SyncNowPlayingCommand, NowPlayingResult>, SyncNowPlayingHandler>();

        // Register Query Handlers
        services.AddScoped<IQueryHandler<GetNowPlayingQuery, NowPlayingResult>, GetNowPlayingHandler>();
        services.AddScoped<IQueryHandler<GetNowPlayingHistoryQuery, List<NowPlayingHistoryResult>>, GetNowPlayingHistoryHandler>();

        return services;
    }
}
