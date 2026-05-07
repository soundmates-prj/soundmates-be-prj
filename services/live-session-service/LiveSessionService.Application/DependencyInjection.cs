using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Features.LiveSessions.Commands.CancelSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.CreateLiveSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.CreateSessionSchedule;
using LiveSessionService.Application.Features.LiveSessions.Commands.DeleteSessionSchedule;
using LiveSessionService.Application.Features.LiveSessions.Commands.PauseSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.ResumeSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.StartSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.StopSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.UpdateLiveSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.UpdateSessionSchedule;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetActiveLiveSessions;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetAllLiveSessions;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetAllSessionSchedules;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSession;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionNowPlaying;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetSessionSchedules;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetScheduleById;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetStaffDashboardOverview;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetHostDashboardOverview;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetHostAnalyticsOverview;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetStaffAnalyticsOverview;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionQueue;
using LiveSessionService.Application.Features.SongRequests.Queries.GetAllSongRequests;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionChats;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionStatistics;
using LiveSessionService.Application.Features.LiveSessions.Commands.RestartBroadcast;
using LiveSessionService.Application.Features.LiveSessions.Commands.SkipTrack;
using LiveSessionService.Application.Features.LiveSessions.Queries.SearchSchedules;
using LiveSessionService.Application.Features.Music.Commands.BulkUploadMusic;
using LiveSessionService.Application.Features.Music.Commands.DeleteMedia;
using LiveSessionService.Application.Features.Music.Commands.ImportSystemMediaBatch;
using LiveSessionService.Application.Features.Music.Commands.SyncMediaFiles;
using LiveSessionService.Application.Features.Music.Commands.UpdateMusicMetadata;
using LiveSessionService.Application.Features.Music.Commands.UploadMusic;
using LiveSessionService.Application.Features.Music.Queries.GetAllMediaFiles;
using LiveSessionService.Application.Features.Music.Queries.GetMediaFilesByStation;
using LiveSessionService.Application.Features.NowPlaying.Commands.SyncNowPlaying;
using LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlaying;
using LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlayingHistory;
using LiveSessionService.Application.Features.PodcastRequests.Commands.CancelPodcastRequest;
using LiveSessionService.Application.Features.PodcastRequests.Commands.CreatePodcastRequest;
using LiveSessionService.Application.Features.PodcastRequests.Commands.ReviewPodcastRequest;
using LiveSessionService.Application.Features.PodcastRequests.Queries.GetMyPodcastRequests;
using LiveSessionService.Application.Features.PodcastRequests.Queries.GetPodcastRequestById;
using LiveSessionService.Application.Features.PodcastRequests.Queries.GetPodcastRequests;
using LiveSessionService.Application.Features.PodcastEpisodeRequests.Commands.CreatePodcastEpisodeRequest;
using LiveSessionService.Application.Features.PodcastEpisodeRequests.Commands.ReviewPodcastEpisodeRequest;
using LiveSessionService.Application.Features.PodcastEpisodeRequests.Queries.GetPodcastEpisodeRequests;
using LiveSessionService.Application.Features.PodcastEpisodeRequests.Queries.GetMyPodcastEpisodeRequests;
using LiveSessionService.Application.Features.PodcastEpisodeRequests.Queries.GetPodcastEpisodeRequestById;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Application.Features.Playlists.Commands.AddMediaToPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.AddTracksToUserPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.CreatePlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.CreateUserPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.DeletePlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.DeleteUserPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.RemoveMediaFromPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.RemoveTracksFromUserPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.SyncPlaylists;
using LiveSessionService.Application.Features.Playlists.Commands.UpdatePlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.UpdateUserPlaylist;
using LiveSessionService.Application.Features.Playlists.Queries.GetPlaylistsByStation;
using LiveSessionService.Application.Features.Playlists.Queries.GetPlaylistTracks;
using LiveSessionService.Application.Features.Playlists.Queries.GetAllPublicUserPlaylists;
using LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylistById;
using LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylists;
using LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylistTracks;
using LiveSessionService.Application.Features.Playlists.Queries.GetPublicUserPlaylistsByUserId;
using LiveSessionService.Application.Features.Podcasts.Commands.CreatePodcast;
using LiveSessionService.Application.Features.Podcasts.Commands.CreatePodcastEpisode;
using LiveSessionService.Application.Features.Podcasts.Commands.DeletePodcast;
using LiveSessionService.Application.Features.Podcasts.Commands.DeletePodcastEpisode;
using LiveSessionService.Application.Features.Podcasts.Commands.UpdatePodcast;
using LiveSessionService.Application.Features.Podcasts.Commands.UpdatePodcastEpisode;
using LiveSessionService.Application.Features.Podcasts.Commands.FollowPodcast;
using LiveSessionService.Application.Features.Podcasts.Commands.UnfollowPodcast;
using LiveSessionService.Application.Features.Podcasts.Queries.GetPodcast;
using LiveSessionService.Application.Features.Podcasts.Queries.GetPodcastEpisodeById;
using LiveSessionService.Application.Features.Podcasts.Queries.GetPodcastEpisodes;
using LiveSessionService.Application.Features.Podcasts.Queries.GetPodcasts;
using LiveSessionService.Application.Features.Podcasts.Queries.GetFollowedPodcasts;
using LiveSessionService.Application.Features.Podcasts.Queries.GetMyPodcasts;
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
using LiveSessionService.Application.Features.SongRequests.Queries.GetMySongRequestLimits;
using LiveSessionService.Application.Features.SongRequests.Queries.GetSongRequestsBySession;
using LiveSessionService.Application.Features.Stations.Commands.CreateStation;
using LiveSessionService.Application.Features.Stations.Commands.SyncStations;
using LiveSessionService.Application.Features.Stations.Commands.RestartStation;
using LiveSessionService.Application.Features.Stations.Commands.ReloadStation;
using LiveSessionService.Application.Features.LiveSessions.Commands.ReloadBroadcast;
using LiveSessionService.Application.Features.Stations.Queries.GetAllStations;
using LiveSessionService.Application.Features.Stations.Queries.GetStationNowPlaying;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetAdminAnalyticsOverview;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

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
        services.AddScoped<ICommandHandler<RestartStationCommand, bool>, RestartStationHandler>();
        services.AddScoped<ICommandHandler<ReloadStationCommand, bool>, ReloadStationHandler>();

        // Register LiveSession Command Handlers
        services.AddScoped<ICommandHandler<CreateLiveSessionCommand, LiveSessionResult>, CreateLiveSessionHandler>();
        services.AddScoped<ICommandHandler<CreateSessionScheduleCommand, SessionScheduleResult>, CreateSessionScheduleHandler>();
        services.AddScoped<ICommandHandler<UpdateSessionScheduleCommand, SessionScheduleResult>, UpdateSessionScheduleHandler>();
        services.AddScoped<ICommandHandler<UpdateLiveSessionCommand, LiveSessionResult>, UpdateLiveSessionHandler>();
        services.AddScoped<ICommandHandler<CancelSessionCommand, LiveSessionResult>, CancelSessionHandler>();
        services.AddScoped<ICommandHandler<DeleteSessionScheduleCommand>, DeleteSessionScheduleHandler>();
        services.AddScoped<ICommandHandler<StartSessionCommand, LiveSessionResult>, StartSessionHandler>();
        services.AddScoped<ICommandHandler<PauseSessionCommand, LiveSessionResult>, PauseSessionHandler>();
        services.AddScoped<ICommandHandler<ResumeSessionCommand, LiveSessionResult>, ResumeSessionHandler>();
        services.AddScoped<ICommandHandler<StopSessionCommand, LiveSessionResult>, StopSessionHandler>();
        services.AddScoped<ICommandHandler<SkipSessionTrackCommand, bool>, SkipSessionTrackHandler>();
        services.AddScoped<ICommandHandler<RestartSessionBroadcastCommand, bool>, RestartSessionBroadcastHandler>();
        services.AddScoped<ICommandHandler<ReloadSessionBroadcastCommand, bool>, ReloadSessionBroadcastHandler>();

        // Register NowPlaying Query Handlers
        services.AddScoped<IQueryHandler<GetNowPlayingQuery, NowPlayingResult>, GetNowPlayingHandler>();
        services.AddScoped<IQueryHandler<GetNowPlayingHistoryQuery, PagedResult<NowPlayingHistoryResult>>, GetNowPlayingHistoryHandler>();

        // Register Station Query Handlers
        services.AddScoped<IQueryHandler<GetAllStationsQuery, List<StationResult>>, GetAllStationsHandler>();
        services.AddScoped<IQueryHandler<GetStationNowPlayingQuery, StationNowPlayingResult>, GetStationNowPlayingHandler>();

        // Register Playlist Command Handlers
        services.AddScoped<ICommandHandler<CreatePlaylistCommand, PlaylistResult>, CreatePlaylistHandler>();
        services.AddScoped<ICommandHandler<UpdatePlaylistCommand, PlaylistResult>, UpdatePlaylistHandler>();
        services.AddScoped<ICommandHandler<DeletePlaylistCommand>, DeletePlaylistHandler>();
        services.AddScoped<ICommandHandler<AddMediaToPlaylistCommand, PlaylistMediaResult>, AddMediaToPlaylistHandler>();
        services.AddScoped<ICommandHandler<RemoveMediaFromPlaylistCommand>, RemoveMediaFromPlaylistHandler>();
        services.AddScoped<ICommandHandler<SyncPlaylistsCommand, SyncPlaylistsResult>, SyncPlaylistsHandler>();
        services.AddScoped<ICommandHandler<CreateUserPlaylistCommand, UserPlaylistResult>, CreateUserPlaylistHandler>();
        services.AddScoped<ICommandHandler<UpdateUserPlaylistCommand, UserPlaylistResult>, UpdateUserPlaylistHandler>();
        services.AddScoped<ICommandHandler<DeleteUserPlaylistCommand>, DeleteUserPlaylistHandler>();
        services.AddScoped<ICommandHandler<AddTracksToUserPlaylistCommand, List<PlaylistMediaResult>>, AddTracksToUserPlaylistHandler>();
        services.AddScoped<ICommandHandler<RemoveTracksFromUserPlaylistCommand>, RemoveTracksFromUserPlaylistHandler>();

        // Register Music Command Handlers
        services.AddHttpClient(); // Used by ImportSystemMediaBatchHandler to download Cloudinary files
        services.AddScoped<ICommandHandler<UploadMusicCommand, MusicResult>, UploadMusicHandler>();
        services.AddScoped<ICommandHandler<BulkUploadMusicCommand, BulkUploadMusicResult>, BulkUploadMusicHandler>();
        services.AddScoped<ICommandHandler<SyncMediaFilesCommand, SyncMediaFilesResult>, SyncMediaFilesHandler>();
        services.AddScoped<ICommandHandler<ImportSystemMediaBatchCommand, ImportSystemMediaBatchResult>, ImportSystemMediaBatchHandler>();
        services.AddScoped<ICommandHandler<DeleteMediaCommand>, DeleteMediaHandler>();
        services.AddScoped<ICommandHandler<UpdateMusicMetadataCommand, MusicResult>, UpdateMusicMetadataHandler>();

        // Register Music Query Handlers
        services.AddScoped<IQueryHandler<GetAllMediaFilesQuery, List<MusicResult>>, GetAllMediaFilesHandler>();
        services.AddScoped<IQueryHandler<GetMediaFilesByStationQuery, List<MusicResult>>, GetMediaFilesByStationHandler>();

        // Register LiveSession Query Handlers
        services.AddScoped<IQueryHandler<GetLiveSessionQuery, LiveSessionResult>, GetLiveSessionHandler>();
        services.AddScoped<IQueryHandler<GetLiveSessionNowPlayingQuery, StationNowPlayingResult>, GetLiveSessionNowPlayingHandler>();
        services.AddScoped<IQueryHandler<GetLiveSessionQueueQuery, LiveSessionQueueResult>, GetLiveSessionQueueHandler>();
        services.AddScoped<IQueryHandler<GetAllLiveSessionsQuery, PagedResult<LiveSessionResult>>, GetAllLiveSessionsHandler>();
        services.AddScoped<IQueryHandler<GetSessionSchedulesQuery, List<SessionScheduleResult>>, GetSessionSchedulesHandler>();
        services.AddScoped<IQueryHandler<GetAllSessionSchedulesQuery, List<SessionScheduleResult>>, GetAllSessionSchedulesHandler>();
        services.AddScoped<IQueryHandler<GetScheduleByIdQuery, SessionScheduleResult>, GetScheduleByIdHandler>();
        services.AddScoped<IQueryHandler<SearchSchedulesQuery, SearchSchedulesResult>, SearchSchedulesQueryHandler>();
        services.AddScoped<IQueryHandler<GetStaffDashboardOverviewQuery, StaffDashboardOverviewResult>, GetStaffDashboardOverviewHandler>();
        services.AddScoped<IQueryHandler<GetStaffAnalyticsOverviewQuery, StaffAnalyticsOverviewResult>, GetStaffAnalyticsOverviewHandler>();
        services.AddScoped<IQueryHandler<GetHostDashboardOverviewQuery, HostDashboardOverviewResult>, GetHostDashboardOverviewHandler>();
        services.AddScoped<IQueryHandler<GetHostAnalyticsOverviewQuery, HostAnalyticsOverviewResult>, GetHostAnalyticsOverviewHandler>();
        services.AddScoped<IQueryHandler<GetAdminAnalyticsOverviewQuery, AdminAnalyticsOverviewResult>, GetAdminAnalyticsOverviewHandler>();
        services.AddScoped<IQueryHandler<GetActiveLiveSessionsQuery, List<LiveSessionResult>>, GetActiveLiveSessionsHandler>();
        services.AddScoped<IQueryHandler<GetLiveSessionStatisticsQuery, LiveSessionStatisticsResult>, GetLiveSessionStatisticsHandler>();
        services.AddScoped<IQueryHandler<GetLiveSessionChatsQuery, List<LiveSessionChatResult>>, GetLiveSessionChatsHandler>();

        // Register Playlist Query Handlers
        services.AddScoped<IQueryHandler<GetPlaylistsByStationQuery, List<PlaylistResult>>, GetPlaylistsByStationHandler>();
        services.AddScoped<IQueryHandler<GetAllPublicUserPlaylistsQuery, List<UserPlaylistResult>>, GetAllPublicUserPlaylistsHandler>();
        services.AddScoped<IQueryHandler<GetUserPlaylistsQuery, List<UserPlaylistResult>>, GetUserPlaylistsHandler>();
        services.AddScoped<IQueryHandler<GetPublicUserPlaylistsByUserIdQuery, List<UserPlaylistResult>>, GetPublicUserPlaylistsByUserIdHandler>();
        services.AddScoped<IQueryHandler<GetUserPlaylistByIdQuery, UserPlaylistResult>, GetUserPlaylistByIdHandler>();
        services.AddScoped<IQueryHandler<GetPlaylistTracksQuery, List<PlaylistMediaResult>>, GetPlaylistTracksHandler>();
        services.AddScoped<IQueryHandler<GetUserPlaylistTracksQuery, List<PlaylistMediaResult>>, GetUserPlaylistTracksHandler>();

        // Register Podcast Command Handlers
        services.AddScoped<ICommandHandler<CreatePodcastCommand, PodcastResult>, CreatePodcastHandler>();
        services.AddScoped<ICommandHandler<LiveSessionService.Application.Features.Podcasts.Commands.GrantPodcastAccess.GrantPodcastAccessCommand>, LiveSessionService.Application.Features.Podcasts.Commands.GrantPodcastAccess.GrantPodcastAccessHandler>();
        services.AddScoped<ICommandHandler<FollowPodcastCommand, PodcastResult>, FollowPodcastHandler>();
        services.AddScoped<ICommandHandler<UnfollowPodcastCommand>, UnfollowPodcastHandler>();
        services.AddScoped<ICommandHandler<UpdatePodcastCommand, PodcastResult>, UpdatePodcastHandler>();
        services.AddScoped<ICommandHandler<DeletePodcastCommand>, DeletePodcastHandler>();
        services.AddScoped<ICommandHandler<CreatePodcastEpisodeCommand, PodcastEpisodeResult>, CreatePodcastEpisodeHandler>();
        services.AddScoped<ICommandHandler<UpdatePodcastEpisodeCommand, PodcastEpisodeResult>, UpdatePodcastEpisodeHandler>();
        services.AddScoped<ICommandHandler<DeletePodcastEpisodeCommand>, DeletePodcastEpisodeHandler>();

        // Register Podcast Query Handlers
        services.AddScoped<IQueryHandler<GetPodcastQuery, PodcastResult>, GetPodcastHandler>();
        services.AddScoped<IQueryHandler<GetFollowedPodcastsQuery, List<PodcastResult>>, GetFollowedPodcastsHandler>();
        services.AddScoped<IQueryHandler<GetPodcastsQuery, List<PodcastResult>>, GetPodcastsHandler>();
        services.AddScoped<IQueryHandler<GetPodcastEpisodesQuery, List<PodcastEpisodeResult>>, GetPodcastEpisodesHandler>();
        services.AddScoped<IQueryHandler<GetPodcastEpisodeByIdQuery, PodcastEpisodeResult>, GetPodcastEpisodeByIdHandler>();
        services.AddScoped<IQueryHandler<GetMyPodcastsQuery, List<PodcastResult>>, GetMyPodcastsHandler>();

        // Register SongRequest Command Handlers
        services.AddScoped<ICommandHandler<CreateSongRequestCommand, SongRequestResult>, CreateSongRequestHandler>();
        services.AddScoped<ICommandHandler<ReviewSongRequestCommand, SongRequestResult>, ReviewSongRequestHandler>();

        // Register SongRequest Query Handlers
        services.AddScoped<IQueryHandler<GetSongRequestsBySessionQuery, List<SongRequestResult>>, GetSongRequestsBySessionHandler>();
        services.AddScoped<IQueryHandler<GetAllSongRequestsQuery, PagedResult<SongRequestResult>>, GetAllSongRequestsHandler>();
        services.AddScoped<IQueryHandler<GetMySongRequestLimitsQuery, MySongRequestLimitsResult>, GetMySongRequestLimitsQueryHandler>();

        // Register PodcastRequest Command Handlers
        services.AddScoped<ICommandHandler<CreatePodcastRequestCommand, PodcastRequestResult>, CreatePodcastRequestHandler>();
        services.AddScoped<ICommandHandler<ReviewPodcastRequestCommand, PodcastRequestResult>, ReviewPodcastRequestHandler>();
        services.AddScoped<ICommandHandler<CancelPodcastRequestCommand, bool>, CancelPodcastRequestHandler>();

        // Register PodcastRequest Query Handlers
        services.AddScoped<IQueryHandler<GetPodcastRequestsQuery, List<PodcastRequestResult>>, GetPodcastRequestsHandler>();
        services.AddScoped<IQueryHandler<GetMyPodcastRequestsQuery, List<PodcastRequestResult>>, GetMyPodcastRequestsHandler>();
        services.AddScoped<IQueryHandler<GetPodcastRequestByIdQuery, PodcastRequestResult>, GetPodcastRequestByIdHandler>();

        // Register PodcastEpisodeRequest Command Handlers
        services.AddScoped<ICommandHandler<CreatePodcastEpisodeRequestCommand, PodcastEpisodeRequestResult>, CreatePodcastEpisodeRequestHandler>();
        services.AddScoped<ICommandHandler<ReviewPodcastEpisodeRequestCommand, PodcastEpisodeRequestResult>, ReviewPodcastEpisodeRequestHandler>();

        // Register PodcastEpisodeRequest Query Handlers
        services.AddScoped<IQueryHandler<GetPodcastEpisodeRequestsQuery, List<PodcastEpisodeRequestResult>>, GetPodcastEpisodeRequestsHandler>();
        services.AddScoped<IQueryHandler<GetMyPodcastEpisodeRequestsQuery, List<PodcastEpisodeRequestResult>>, GetMyPodcastEpisodeRequestsHandler>();
        services.AddScoped<IQueryHandler<GetPodcastEpisodeRequestByIdQuery, PodcastEpisodeRequestResult>, GetPodcastEpisodeRequestByIdHandler>();

        // Register Sync Services
        services.AddScoped<LiveSessionService.Application.Services.IInputValidationService, LiveSessionService.Application.Services.InputValidationService>();
        services.AddSingleton<LiveSessionService.Application.Services.ISyncConfigurationService, LiveSessionService.Application.Services.SyncConfigurationService>();
        services.AddScoped<LiveSessionService.Application.Services.IAzuraCastErrorHandler, LiveSessionService.Application.Services.AzuraCastErrorHandler>();

        return services;
    }
}
