using Microsoft.Extensions.DependencyInjection;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Features.NowPlaying.Commands.SyncNowPlaying;
using LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlaying;
using LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlayingHistory;
using LiveSessionService.Application.Features.Stations.Commands.SyncStations;
using LiveSessionService.Application.Features.Stations.Queries.GetAllStations;
using LiveSessionService.Application.Features.LiveSessions.Commands.CreateLiveSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.StartSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.StopSession;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSession;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetAllLiveSessions;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Application.Features.Results.Stations;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register Dispatchers (key pattern for CQRS)
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // Register NowPlaying Command Handlers
        services.AddScoped<ICommandHandler<SyncNowPlayingCommand, NowPlayingResult>, SyncNowPlayingHandler>();

        // Register Station Command Handlers
        services.AddScoped<ICommandHandler<SyncStationsCommand, SyncStationsResult>, SyncStationsHandler>();

        // Register LiveSession Command Handlers
        services.AddScoped<ICommandHandler<CreateLiveSessionCommand, LiveSessionResult>, CreateLiveSessionHandler>();
        services.AddScoped<ICommandHandler<StartSessionCommand, LiveSessionResult>, StartSessionHandler>();
        services.AddScoped<ICommandHandler<StopSessionCommand, LiveSessionResult>, StopSessionHandler>();

        // Register NowPlaying Query Handlers
        services.AddScoped<IQueryHandler<GetNowPlayingQuery, NowPlayingResult>, GetNowPlayingHandler>();
        services.AddScoped<IQueryHandler<GetNowPlayingHistoryQuery, PagedResult<NowPlayingHistoryResult>>, GetNowPlayingHistoryHandler>();

        // Register Station Query Handlers
        services.AddScoped<IQueryHandler<GetAllStationsQuery, List<StationResult>>, GetAllStationsHandler>();

        // Register LiveSession Query Handlers
        services.AddScoped<IQueryHandler<GetLiveSessionQuery, LiveSessionResult>, GetLiveSessionHandler>();
        services.AddScoped<IQueryHandler<GetAllLiveSessionsQuery, PagedResult<LiveSessionResult>>, GetAllLiveSessionsHandler>();

        return services;
    }
}
