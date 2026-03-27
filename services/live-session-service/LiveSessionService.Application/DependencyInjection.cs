using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Features.LiveSessions.Commands.CreateLiveSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.CreateSessionSchedule;
using LiveSessionService.Application.Features.LiveSessions.Commands.DeleteSessionSchedule;
using LiveSessionService.Application.Features.LiveSessions.Commands.PauseSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.ResumeSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.StartSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.StopSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.UpdateSessionSchedule;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetActiveLiveSessions;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetAllLiveSessions;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetAllSessionSchedules;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSession;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionNowPlaying;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetSessionSchedules;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetScheduleById;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetStaffDashboardOverview;
using LiveSessionService.Application.Features.Music.Commands.BulkUploadMusic;
using LiveSessionService.Application.Features.Music.Commands.DeleteMedia;
using LiveSessionService.Application.Features.Music.Commands.SyncMediaFiles;
using LiveSessionService.Application.Features.Music.Commands.UploadMusic;
using LiveSessionService.Application.Features.Music.Queries.GetAllMediaFiles;
using LiveSessionService.Application.Features.Music.Queries.GetMediaFilesByStation;
using LiveSessionService.Application.Features.NowPlaying.Commands.SyncNowPlaying;
using LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlaying;
using LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlayingHistory;
using LiveSessionService.Application.Features.Playlists.Commands.AddMediaToPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.CreatePlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.CreateUserPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.DeleteUserPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.RemoveMediaFromPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.SyncPlaylists;
using LiveSessionService.Application.Features.Playlists.Commands.UpdatePlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.UpdateUserPlaylist;
using LiveSessionService.Application.Features.Playlists.Queries.GetPlaylistsByStation;
using LiveSessionService.Application.Features.Playlists.Queries.GetPlaylistTracks;
using LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylistById;
using LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylists;
using LiveSessionService.Application.Features.Podcasts.Commands.CreatePodcast;
using LiveSessionService.Application.Features.Podcasts.Commands.DeletePodcast;
using LiveSessionService.Application.Features.Podcasts.Commands.UpdatePodcast;
using LiveSessionService.Application.Features.Podcasts.Queries.GetPodcast;
using LiveSessionService.Application.Features.Podcasts.Queries.GetPodcasts;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Application.Features.Results.Podcasts;
using LiveSessionService.Application.Features.Results.SongRequests;
using LiveSessionService.Application.Features.Results.Stations;
using LiveSessionService.Application.Features.SongRequests.Commands.CreateSongRequest;
using LiveSessionService.Application.Features.SongRequests.Commands.ReviewSongRequest;
using LiveSessionService.Application.Features.SongRequests.Queries.GetSongRequestsBySession;
using LiveSessionService.Application.Features.Stations.Commands.CreateStation;
using LiveSessionService.Application.Features.Stations.Commands.CreateStation;
using LiveSessionService.Application.Features.Stations.Commands.SyncStations;
using LiveSessionService.Application.Features.Stations.Queries.GetAllStations;
using LiveSessionService.Application.Features.Stations.Queries.GetStationNowPlaying;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<ICommandHandler<CreateStationCommand, StationResult>, CreateStationHandler>();
        services.AddScoped<ICommandHandler<SyncStationsCommand, SyncStationsResult>, SyncStationsHandler>();

        // Register LiveSession Command Handlers
        services.AddScoped<ICommandHandler<CreateLiveSessionCommand, LiveSessionResult>, CreateLiveSessionHandler>();
        services.AddScoped<ICommandHandler<CreateSessionScheduleCommand, SessionScheduleResult>, CreateSessionScheduleHandler>();
        services.AddScoped<ICommandHandler<UpdateSessionScheduleCommand, SessionScheduleResult>, UpdateSessionScheduleHandler>();
        services.AddScoped<ICommandHandler<DeleteSessionScheduleCommand>, DeleteSessionScheduleHandler>();
        services.AddScoped<ICommandHandler<StartSessionCommand, LiveSessionResult>, StartSessionHandler>();
        services.AddScoped<ICommandHandler<PauseSessionCommand, LiveSessionResult>, PauseSessionHandler>();
        services.AddScoped<ICommandHandler<ResumeSessionCommand, LiveSessionResult>, ResumeSessionHandler>();
        services.AddScoped<ICommandHandler<StopSessionCommand, LiveSessionResult>, StopSessionHandler>();

        // Register NowPlaying Query Handlers
        services.AddScoped<IQueryHandler<GetNowPlayingQuery, NowPlayingResult>, GetNowPlayingHandler>();
        services.AddScoped<IQueryHandler<GetNowPlayingHistoryQuery, PagedResult<NowPlayingHistoryResult>>, GetNowPlayingHistoryHandler>();

        // Register Station Query Handlers
        services.AddScoped<IQueryHandler<GetAllStationsQuery, List<StationResult>>, GetAllStationsHandler>();
        services.AddScoped<IQueryHandler<GetStationNowPlayingQuery, StationNowPlayingResult>, GetStationNowPlayingHandler>();

        // Register Playlist Command Handlers
        services.AddScoped<ICommandHandler<CreatePlaylistCommand, PlaylistResult>, CreatePlaylistHandler>();
        services.AddScoped<ICommandHandler<CreateUserPlaylistCommand, UserPlaylistResult>, CreateUserPlaylistHandler>();
        services.AddScoped<ICommandHandler<UpdatePlaylistCommand, PlaylistResult>, UpdatePlaylistHandler>();
        services.AddScoped<ICommandHandler<UpdateUserPlaylistCommand, UserPlaylistResult>, UpdateUserPlaylistHandler>();
        services.AddScoped<ICommandHandler<DeleteUserPlaylistCommand>, DeleteUserPlaylistHandler>();
        services.AddScoped<ICommandHandler<AddMediaToPlaylistCommand, PlaylistMediaResult>, AddMediaToPlaylistHandler>();
        services.AddScoped<ICommandHandler<RemoveMediaFromPlaylistCommand>, RemoveMediaFromPlaylistHandler>();
        services.AddScoped<ICommandHandler<SyncPlaylistsCommand, SyncPlaylistsResult>, SyncPlaylistsHandler>();

        // Register Music Command Handlers
        services.AddScoped<ICommandHandler<UploadMusicCommand, MusicResult>, UploadMusicHandler>();
        services.AddScoped<ICommandHandler<BulkUploadMusicCommand, BulkUploadMusicResult>, BulkUploadMusicHandler>();
        services.AddScoped<ICommandHandler<SyncMediaFilesCommand, SyncMediaFilesResult>, SyncMediaFilesHandler>();
        services.AddScoped<ICommandHandler<DeleteMediaCommand>, DeleteMediaHandler>();

        // Register Music Query Handlers
        services.AddScoped<IQueryHandler<GetAllMediaFilesQuery, List<MusicResult>>, GetAllMediaFilesHandler>();
        services.AddScoped<IQueryHandler<GetMediaFilesByStationQuery, List<MusicResult>>, GetMediaFilesByStationHandler>();

        // Register LiveSession Query Handlers
        services.AddScoped<IQueryHandler<GetLiveSessionQuery, LiveSessionResult>, GetLiveSessionHandler>();
        services.AddScoped<IQueryHandler<GetLiveSessionNowPlayingQuery, StationNowPlayingResult>, GetLiveSessionNowPlayingHandler>();
        services.AddScoped<IQueryHandler<GetAllLiveSessionsQuery, PagedResult<LiveSessionResult>>, GetAllLiveSessionsHandler>();
        services.AddScoped<IQueryHandler<GetSessionSchedulesQuery, List<SessionScheduleResult>>, GetSessionSchedulesHandler>();
        services.AddScoped<IQueryHandler<GetAllSessionSchedulesQuery, List<SessionScheduleResult>>, GetAllSessionSchedulesHandler>();
        services.AddScoped<IQueryHandler<GetScheduleByIdQuery, SessionScheduleResult>, GetScheduleByIdHandler>();
        services.AddScoped<IQueryHandler<GetStaffDashboardOverviewQuery, StaffDashboardOverviewResult>, GetStaffDashboardOverviewHandler>();
        services.AddScoped<IQueryHandler<GetActiveLiveSessionsQuery, List<LiveSessionResult>>, GetActiveLiveSessionsHandler>();

        // Register Playlist Query Handlers
        services.AddScoped<IQueryHandler<GetPlaylistsByStationQuery, List<PlaylistResult>>, GetPlaylistsByStationHandler>();
        services.AddScoped<IQueryHandler<GetUserPlaylistsQuery, List<UserPlaylistResult>>, GetUserPlaylistsHandler>();
        services.AddScoped<IQueryHandler<GetUserPlaylistByIdQuery, UserPlaylistResult>, GetUserPlaylistByIdHandler>();
        services.AddScoped<IQueryHandler<GetPlaylistTracksQuery, List<PlaylistMediaResult>>, GetPlaylistTracksHandler>();

        // Register Podcast Command Handlers
        services.AddScoped<ICommandHandler<CreatePodcastCommand, PodcastResult>, CreatePodcastHandler>();
        services.AddScoped<ICommandHandler<UpdatePodcastCommand, PodcastResult>, UpdatePodcastHandler>();
        services.AddScoped<ICommandHandler<DeletePodcastCommand>, DeletePodcastHandler>();

        // Register Podcast Query Handlers
        services.AddScoped<IQueryHandler<GetPodcastQuery, PodcastResult>, GetPodcastHandler>();
        services.AddScoped<IQueryHandler<GetPodcastsQuery, List<PodcastResult>>, GetPodcastsHandler>();

        // Register SongRequest Command Handlers
        services.AddScoped<ICommandHandler<CreateSongRequestCommand, SongRequestResult>, CreateSongRequestHandler>();
        services.AddScoped<ICommandHandler<ReviewSongRequestCommand, SongRequestResult>, ReviewSongRequestHandler>();

        // Register SongRequest Query Handlers
        services.AddScoped<IQueryHandler<GetSongRequestsBySessionQuery, List<SongRequestResult>>, GetSongRequestsBySessionHandler>();

        return services;
    }
}
