# Entity Relationship Diagram

## Core Relationships

```
???????????????????????????????????????????????????????????????????
?                         LiveSession                             ?
?  (Aggregate Root)                                               ?
?  • Id, HostUserId, SessionName, Status                         ?
?  • StartedAt, EndedAt, MaxListeners                            ?
???????????????????????????????????????????????????????????????????
    ?                    ?                ?
    ? 1:N                ? 1:N            ? 1:N
    ?                    ?                ?
    ?                    ?                ?
????????????????  ????????????????  ????????????????????
? Session      ?  ? Session      ?  ? NowPlaying       ?
? Participant  ?  ? Listener     ?  ? History          ?
?              ?  ?              ?  ?                  ?
? • Role       ?  ? • UserId     ?  ? • SongTitle      ?
? • Perms      ?  ? • IpAddress  ?  ? • PlayedAt       ?
????????????????  ? • Country    ?  ? • ListenerPeak   ?
                  ????????????????  ????????????????????


???????????????????????????????????????????????????????????????????
?                     AzuraCastStation                            ?
?  • ExternalStationId, StreamUrl, ApiKey                        ?
?  • SyncStatus, LastSyncedAt                                     ?
??????????????????????????????????????????????????????????????????
    ? 1:N              ? 1:N              ? 1:N
    ?                  ?                  ?
    ?                  ?                  ?
????????????????  ???????????????  ????????????????
? LiveSession  ?  ? Station     ?  ? Station      ?
?              ?  ? Playlist    ?  ? Mount        ?
????????????????  ?             ?  ?              ?
                  ? • Type      ?  ? • MountPath  ?
                  ? • Weight    ?  ? • Bitrate    ?
                  ???????????????  ????????????????
                        ? 1:N
                        ?
                  ???????????????
                  ? Playlist    ?
                  ? Media       ?
                  ?             ?
                  ? • MediaId   ?
                  ? • PlayCount ?
                  ???????????????
```

## Entity Summary Table

| Entity                | Purpose                           | Aggregate Root | Sync Source    |
|-----------------------|-----------------------------------|----------------|----------------|
| LiveSession           | Streaming session management      | ?              | Internal       |
| AzuraCastStation      | AzuraCast station configuration   | ?              | AzuraCast API  |
| NowPlayingHistory     | Track play history                | ?              | AzuraCast API  |
| SessionParticipant    | Host/moderator management         | ?              | Internal       |
| SessionListener       | Listener tracking                 | ?              | SignalR + API  |
| SessionActivity       | Event logging                     | ?              | Internal       |
| StationPlaylist       | Playlist configuration            | ?              | AzuraCast API  |
| PlaylistMedia         | Playlist tracks                   | ?              | AzuraCast API  |
| StationMount          | Stream mount points               | ?              | AzuraCast API  |
| ListenerStatistics    | Aggregated metrics                | ?              | Computed       |
| OutboxMessage         | Event sourcing                    | ?              | Internal       |

## Key Enumerations

### SessionStatus
- `Scheduled` (0): Session created, not yet started
- `Live` (1): Currently streaming
- `Paused` (2): Temporarily paused
- `Ended` (3): Completed normally
- `Cancelled` (4): Cancelled before completion

### ParticipantRole
- `Host` (0): Session creator
- `CoHost` (1): Full management access
- `Moderator` (2): Chat/listener moderation
- `Listener` (3): Regular participant

### ActivityType
- `SessionStarted/Ended/Paused/Resumed` (0-3)
- `UserJoined/Left` (4-5)
- `SongChanged` (6)
- `PlaylistUpdated` (7)
- `ListenerPeakReached` (8)
- `ModeratorAction` (9)
- `ChatMessage` (10)
- `SongRequested` (11)
- `Error` (99)

### StationSyncStatus
- `NotSynced` (0): Never synced
- `Syncing` (1): Sync in progress
- `Synced` (2): Successfully synced
- `Failed` (3): Sync error

### PlaylistType
- `Default` (0): Standard rotation
- `Scheduled` (1): Time-based
- `OncePerHour` (2): Hourly jingle
- `OncePerDay` (3): Daily jingle
- `Advanced` (4): Custom logic
- `Jingle` (5): Station IDs

## Data Flow Patterns

### Pattern 1: Now Playing Sync
```
AzuraCast API ? Background Job ? NowPlayingHistory ? SignalR ? Clients
                                       ?
                                 SessionActivity
                                       ?
                                  OutboxMessage
```

### Pattern 2: Listener Tracking
```
Client ? SignalR Hub ? SessionListener (Created)
                            ?
                    Background Geocoding
                            ?
                    Periodic Updates (30s)
                            ?
                    On Disconnect ? Calculate Duration
```

### Pattern 3: Session Lifecycle
```
Create ? Scheduled ? Allocate Station ? Live ? Sync Loop ? Ended
                                          ?
                                    Update Stats
                                          ?
                                  Publish Events
```

## AzuraCast API Endpoints Reference

| Endpoint                                    | Method | Purpose                  | Frequency       |
|---------------------------------------------|--------|--------------------------|-----------------|
| `/api/station/{id}`                         | GET    | Station metadata         | 5 minutes       |
| `/api/nowplaying/{id}`                      | GET    | Current track            | 5 seconds       |
| `/api/station/{id}/listeners`               | GET    | Listener details         | 30 seconds      |
| `/api/station/{id}/playlists`               | GET    | Playlist list            | 10 minutes      |
| `/api/station/{id}/playlist/{pid}/media`    | GET    | Playlist tracks          | On-demand       |
| `/api/station/{id}/mounts`                  | GET    | Stream mounts            | 5 minutes       |
| `/api/station/{id}/request/{media_id}`      | POST   | Request song             | On-demand       |

## Database Schema Highlights

### Primary Keys
All entities use `Guid` as primary key for distributed system compatibility.

### Timestamps
- `CreatedAt`: Record creation (required)
- `UpdatedAt`: Last modification (nullable)
- `LastSyncedAt`: Last sync from AzuraCast (nullable)

### Soft Deletes
Consider implementing `IsDeleted` flag for historical data preservation.

### Indexes (Recommended)
```sql
-- Performance critical indexes
CREATE INDEX idx_livesession_status_started 
    ON LiveSession(Status, StartedAt);

CREATE INDEX idx_sessionlistener_session_connected 
    ON SessionListener(LiveSessionId, IsConnected);

CREATE INDEX idx_nowplaying_session_played 
    ON NowPlayingHistory(LiveSessionId, PlayedAt DESC);

CREATE INDEX idx_activity_session_occurred 
    ON SessionActivity(LiveSessionId, OccurredAt DESC);

CREATE INDEX idx_station_syncstatus 
    ON AzuraCastStation(SyncStatus, LastSyncedAt);
```

## Event Publishing (Outbox Pattern)

### Events to Publish
1. `SessionStartedEvent`: Notify other services
2. `SessionEndedEvent`: Trigger analytics
3. `ListenerPeakReachedEvent`: Milestone notifications
4. `SongPlayedEvent`: Update user listening history
5. `StationSyncFailedEvent`: Alert admin

### Example Event Structure
```json
{
  "id": "guid",
  "type": "SessionStartedEvent",
  "payload": {
    "sessionId": "guid",
    "hostUserId": "guid",
    "stationId": "guid",
    "startedAt": "2024-01-15T10:30:00Z"
  },
  "occurredAt": "2024-01-15T10:30:00Z"
}
```

## SignalR Groups

### Group Naming Convention
- `session_{sessionId}`: All listeners of a session
- `host_{sessionId}`: Host and co-hosts only
- `user_{userId}`: User-specific notifications

### Broadcasting Examples
```csharp
// To all session listeners
await Clients.Group($"session_{sessionId}")
    .SendAsync("NowPlayingUpdated", data);

// To session hosts only
await Clients.Group($"host_{sessionId}")
    .SendAsync("PrivateMessage", data);

// To specific user
await Clients.User(userId.ToString())
    .SendAsync("Notification", data);
```

## Implementation Checklist

### Phase 1: Core Entities
- [x] Create all entity classes
- [ ] Configure EF Core relationships
- [ ] Add validation rules
- [ ] Implement domain events

### Phase 2: AzuraCast Integration
- [ ] Create AzuraCast API client
- [ ] Implement sync jobs
- [ ] Add webhook handlers
- [ ] Error handling & retry logic

### Phase 3: SignalR Hubs
- [ ] Create LiveSessionHub
- [ ] Implement connection tracking
- [ ] Add group management
- [ ] Configure backplane (Redis)

### Phase 4: CQRS Commands/Queries
- [ ] CreateLiveSessionCommand
- [ ] StartLiveSessionCommand
- [ ] EndLiveSessionCommand
- [ ] GetLiveSessionQuery
- [ ] GetSessionStatisticsQuery

### Phase 5: Background Jobs
- [ ] NowPlayingSyncJob
- [ ] ListenerSyncJob
- [ ] StationMetadataSyncJob
- [ ] OutboxProcessorJob
- [ ] StatisticsAggregationJob

### Phase 6: Testing & Monitoring
- [ ] Unit tests for domain logic
- [ ] Integration tests for API
- [ ] Load testing for SignalR
- [ ] Monitoring dashboards
- [ ] Alert configuration
