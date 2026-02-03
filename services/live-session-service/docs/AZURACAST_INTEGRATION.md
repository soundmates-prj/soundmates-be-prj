# Live Session Service - AzuraCast Integration Documentation

## Overview

The Live Session Service integrates with AzuraCast to provide real-time radio streaming capabilities within the SoundMates platform. This service manages live audio sessions, tracks listener statistics, handles now-playing information, and synchronizes data between AzuraCast stations and the SoundMates ecosystem.

## Architecture

### Clean Architecture Layers

```
???????????????????????????????????????????
?         API Layer (SignalR Hubs)        ?
?  - Real-time communication              ?
?  - REST endpoints for configuration     ?
???????????????????????????????????????????
                  ?
???????????????????????????????????????????
?       Application Layer (CQRS)          ?
?  - Commands/Queries                     ?
?  - Event Handlers                       ?
?  - DTOs                                 ?
???????????????????????????????????????????
                  ?
???????????????????????????????????????????
?         Domain Layer (DDD)              ?
?  - Entities                             ?
?  - Value Objects                        ?
?  - Domain Events                        ?
???????????????????????????????????????????
                  ?
???????????????????????????????????????????
?      Infrastructure Layer               ?
?  - AzuraCast API Client                 ?
?  - Database (EF Core)                   ?
?  - Message Bus (RabbitMQ)               ?
???????????????????????????????????????????
```

---

## Domain Entities

### 1. LiveSession (Aggregate Root)

**Purpose**: Represents a live streaming session hosted by a user, powered by an AzuraCast station.

**Key Properties**:
- `Id`: Unique identifier
- `HostUserId`: User who created the session
- `AzuraCastStationId`: Associated AzuraCast station
- `SessionName`: Display name
- `Status`: Current state (Scheduled, Live, Paused, Ended, Cancelled)
- `StartedAt/EndedAt`: Session timing
- `MaxListeners`: Capacity limit
- `IsPublic`: Visibility flag

**Relationships**:
- One-to-Many with `SessionParticipant` (co-hosts, moderators)
- One-to-Many with `SessionListener` (active listeners)
- One-to-Many with `NowPlayingHistory` (track history)
- One-to-Many with `SessionActivity` (event log)
- Many-to-One with `AzuraCastStation`

**Use Cases**:
- Create new live session
- Start/pause/end session
- Update session metadata
- Monitor listener count
- Track session history

---

### 2. AzuraCastStation

**Purpose**: Represents an AzuraCast radio station configured for the platform.

**Key Properties**:
- `ExternalStationId`: AzuraCast's internal station ID
- `StationName`: Station display name
- `StreamUrl`: Direct stream URL
- `ApiBaseUrl`: AzuraCast instance URL
- `ApiKey`: Authentication token
- `SyncStatus`: Current synchronization state
- `LastSyncedAt`: Last successful sync timestamp

**Relationships**:
- One-to-Many with `LiveSession`
- One-to-Many with `StationPlaylist`
- One-to-Many with `StationMount`

**Synchronization**:
- Periodic sync with AzuraCast API
- Retrieves station metadata, playlists, and mounts
- Updates stream URLs and configuration

**Use Cases**:
- Register new AzuraCast station
- Sync station data from AzuraCast
- Update station configuration
- Monitor station health

---

### 3. NowPlayingHistory

**Purpose**: Tracks songs played during a live session, synced from AzuraCast's "now playing" API.

**Key Properties**:
- `AzuraCastSongHistoryId`: Reference to AzuraCast song history
- `SongTitle/Artist/Album`: Track metadata
- `SongArtUrl`: Album artwork URL
- `PlayedAt/EndedAt`: Play timestamps
- `ListenerPeak`: Maximum concurrent listeners during track
- `IsRequest`: Whether track was requested by user

**Data Flow**:
1. AzuraCast plays track
2. Service polls or webhooks from AzuraCast "now playing" endpoint
3. Entity created/updated with track info
4. SignalR broadcasts to connected clients
5. Listener stats captured at track end

**Use Cases**:
- Display current track to listeners
- Show play history
- Generate session reports
- Track user requests

---

### 4. SessionParticipant

**Purpose**: Represents users with special roles in a session (host, co-host, moderator).

**Roles**:
- `Host`: Session creator, full control
- `CoHost`: Can manage playlists and settings
- `Moderator`: Can manage listeners and chat
- `Listener`: Regular participant (tracked separately)

**Key Properties**:
- `Role`: Participant role
- `CanManagePlaylist`: Permission flag
- `CanModerate`: Permission flag
- `JoinedAt/LeftAt`: Participation duration
- `IsActive`: Current connection status

**Use Cases**:
- Assign co-hosts/moderators
- Control playlist management
- Manage chat moderation
- Track participant history

---

### 5. SessionListener

**Purpose**: Tracks individual listeners connected to a live session.

**Key Properties**:
- `UserId`: Authenticated user (nullable for anonymous)
- `AnonymousIdentifier`: Cookie/token for anonymous users
- `IpAddress`: Client IP (for geolocation)
- `UserAgent`: Browser/device info
- `Country/City`: Geolocation data
- `ConnectedAt/DisconnectedAt`: Connection duration
- `DurationSeconds`: Total listen time

**Data Collection**:
- SignalR connection tracking
- AzuraCast listener API sync
- Real-time connection monitoring
- Geographic analytics

**Use Cases**:
- Real-time listener count
- Geographic distribution
- Listen duration analytics
- Audience insights

---

### 6. SessionActivity

**Purpose**: Event log for session activities and system events.

**Activity Types**:
- Session lifecycle (started, paused, ended)
- User actions (joined, left)
- Content events (song changed, playlist updated)
- Moderation actions
- System events (errors, warnings)

**Key Properties**:
- `ActivityType`: Enum of event types
- `ActivityData`: JSON payload with event details
- `Message`: Human-readable description
- `OccurredAt`: Event timestamp

**Use Cases**:
- Session timeline/history
- Audit logging
- Debugging
- Analytics

---

### 7. StationPlaylist

**Purpose**: Represents playlists configured in AzuraCast, synced to service.

**Playlist Types**:
- `Default`: Standard rotation
- `Scheduled`: Time-based
- `OncePerHour/Day`: Special rotation
- `Advanced`: Custom scheduling
- `Jingle`: Station IDs/ads

**Key Properties**:
- `ExternalPlaylistId`: AzuraCast playlist ID
- `PlaylistName`: Display name
- `Type`: Playlist behavior type
- `Weight`: Rotation priority
- `IsEnabled`: Active status

**Synchronization**:
- Periodic sync from AzuraCast
- Tracks playlist configuration
- Monitors playlist media

**Use Cases**:
- Display available playlists
- Allow users to queue from playlists
- Monitor playlist rotation
- Sync playlist updates

---

### 8. PlaylistMedia

**Purpose**: Individual tracks within station playlists.

**Key Properties**:
- `MediaId`: AzuraCast media ID
- `SongTitle/Artist/Album`: Track metadata
- `DurationSeconds`: Track length
- `PlayCount`: Number of plays
- `Weight`: Selection priority
- `IsEnabled`: Active in rotation

**Use Cases**:
- Browse playlist content
- Enable song requests
- Track play statistics
- Display track information

---

### 9. StationMount

**Purpose**: Represents AzuraCast stream mount points (different bitrates/formats).

**Key Properties**:
- `MountPath`: URL path (e.g., `/radio.mp3`)
- `MountUrl`: Full stream URL
- `Bitrate`: Audio quality (kbps)
- `Format`: Audio codec (MP3, AAC, OGG)
- `CurrentListeners`: Active connections
- `IsDefault`: Primary mount point

**Use Cases**:
- Provide multiple stream qualities
- Monitor mount point usage
- Optimize bandwidth
- Support different clients

---

### 10. ListenerStatistics

**Purpose**: Aggregated listener metrics over time.

**Key Properties**:
- `Timestamp`: Snapshot time
- `CurrentListeners`: Active count
- `UniqueListeners`: Unique users/IPs
- `PeakListeners`: Maximum concurrent
- `AverageListenTimeSeconds`: Engagement metric
- `TopCountry/City`: Geographic leader

**Collection Frequency**: Every 5-15 minutes during live session

**Use Cases**:
- Real-time dashboard
- Historical analytics
- Performance monitoring
- Audience insights

---

### 11. OutboxMessage

**Purpose**: Transactional outbox pattern for reliable event publishing.

**Key Properties**:
- `Type`: Event type
- `Payload`: Serialized event data
- `OccurredAt`: Event timestamp
- `ProcessedAt`: Publish timestamp
- `RetryCount`: Failure handling

**Pattern**:
1. Domain event occurs
2. Event saved to outbox in same transaction
3. Background worker publishes to message bus
4. Ensures no event loss

**Use Cases**:
- Publish session events
- Cross-service communication
- Audit trail
- Failure recovery

---

## AzuraCast Integration Points

### 1. Station Configuration API

**Endpoint**: `GET /api/station/{station_id}`

**Purpose**: Retrieve station metadata and configuration

**Sync Frequency**: On-demand or every 5 minutes

**Data Synced**:
- Station name and description
- Stream URLs
- Mount points
- Playlist configuration

---

### 2. Now Playing API

**Endpoint**: `GET /api/nowplaying/{station_id}`

**Purpose**: Get current track and listener information

**Sync Frequency**: Every 5-10 seconds during live session

**Data Synced**:
- Current track (title, artist, album, art)
- Listener count
- Song duration and elapsed time
- Play history

**SignalR Broadcasting**:
```csharp
// Broadcast to session clients
await Clients.Group($"session_{sessionId}")
    .SendAsync("NowPlayingUpdated", nowPlayingDto);
```

---

### 3. Listeners API

**Endpoint**: `GET /api/station/{station_id}/listeners`

**Purpose**: Get detailed listener information

**Sync Frequency**: Every 30 seconds

**Data Collected**:
- IP addresses (for geolocation)
- Connection duration
- User agents
- Mount point usage

---

### 4. Request API

**Endpoint**: `POST /api/station/{station_id}/request/{media_id}`

**Purpose**: Allow users to request songs

**Flow**:
1. User selects song from playlist
2. Service validates permissions
3. Request sent to AzuraCast
4. Success/failure logged in `SessionActivity`

---

### 5. Playlists API

**Endpoint**: `GET /api/station/{station_id}/playlists`

**Purpose**: Retrieve available playlists and media

**Sync Frequency**: Every 10 minutes or on-demand

**Data Synced**:
- Playlist metadata
- Media items
- Scheduling configuration

---

### 6. Webhooks (Optional)

**AzuraCast Webhook Configuration**:
- Trigger: Song change, listener connect/disconnect
- Target: `https://your-service/api/webhooks/azuracast`
- Authentication: Shared secret

**Events**:
- `song.change`: New track playing
- `listener.connect`: New listener
- `listener.disconnect`: Listener left

---

## Real-Time SignalR Hubs

### LiveSessionHub

**Client Methods**:
- `JoinSession(sessionId)`: Connect to session
- `LeaveSession(sessionId)`: Disconnect from session
- `SendChatMessage(sessionId, message)`: Chat functionality

**Server Events**:
- `NowPlayingUpdated`: New track started
- `ListenerCountUpdated`: Listener count changed
- `SessionStatusChanged`: Session started/paused/ended
- `ChatMessageReceived`: New chat message
- `ParticipantJoined/Left`: User joined/left

---

## Key Workflows

### Starting a Live Session

1. User creates session via API
2. `LiveSession` entity created with status `Scheduled`
3. System allocates `AzuraCastStation` (load balancing)
4. Station stream URL returned to client
5. Host starts streaming to AzuraCast
6. Service detects streaming, updates status to `Live`
7. Now playing sync begins
8. SignalR broadcasts session live event

---

### Tracking Now Playing

1. Background job polls AzuraCast every 5 seconds
2. Detects song change via `current_song.id`
3. Creates `NowPlayingHistory` record
4. Updates previous track's `EndedAt` and `ListenerPeak`
5. Broadcasts to SignalR clients
6. Creates `SessionActivity` entry

---

### Listener Tracking

1. User connects via SignalR
2. `SessionListener` entity created
3. IP geocoded (background job)
4. Periodic updates every 30 seconds
5. On disconnect:
   - `DisconnectedAt` set
   - `DurationSeconds` calculated
   - `IsConnected` set to false

---

### Synchronization Strategy

**Incremental Sync**:
- Station metadata: Every 5 minutes
- Now playing: Every 5 seconds (during live)
- Listeners: Every 30 seconds
- Playlists: Every 10 minutes

**Webhook Sync**:
- Instant updates via AzuraCast webhooks
- Reduces polling frequency
- More accurate real-time data

**Failure Handling**:
- Retry with exponential backoff
- Log sync errors in `AzuraCastStation.LastSyncError`
- Alert on repeated failures
- Fallback to cached data

---

## Performance Considerations

### Database Indexing

```sql
-- High-frequency queries
CREATE INDEX idx_livesession_status ON LiveSession(Status);
CREATE INDEX idx_sessionlistener_session_connected 
    ON SessionListener(LiveSessionId, IsConnected);
CREATE INDEX idx_nowplayinghistory_session_played 
    ON NowPlayingHistory(LiveSessionId, PlayedAt);
```

### Caching Strategy

- **Station Configuration**: Redis cache, 5-minute TTL
- **Now Playing**: In-memory cache, 5-second TTL
- **Listener Count**: In-memory, updated every 5 seconds

### SignalR Scaling

- Use Redis backplane for multi-instance deployment
- Configure sticky sessions on load balancer
- Monitor connection count per instance

---

## Security

### AzuraCast API Authentication

- Store API keys encrypted in database
- Rotate keys periodically
- Use HTTPS for all API calls
- Validate webhook signatures

### User Authorization

- Verify session ownership for management actions
- Rate limit song requests
- Validate permissions for playlist management

---

## Monitoring & Observability

### Key Metrics

- Active sessions count
- Total listeners (current)
- AzuraCast API latency
- Sync success rate
- SignalR connection count

### Health Checks

- AzuraCast station reachability
- Database connectivity
- Message bus status
- Stream availability

### Logging

- Session lifecycle events
- AzuraCast API errors
- SignalR connection issues
- Song request audit trail

---

## Error Handling

### AzuraCast API Failures

- **503 Service Unavailable**: Retry with backoff
- **401 Unauthorized**: Alert admin, check API key
- **404 Not Found**: Station may be deleted, mark inactive
- **Rate Limited**: Implement exponential backoff

### Stream Interruptions

- Monitor station status
- Notify host of issues
- Log in `SessionActivity`
- Attempt automatic recovery

---

## Future Enhancements

1. **Advanced Analytics**:
   - Listener demographics
   - Track popularity metrics
   - Geographic heatmaps

2. **Enhanced Interactivity**:
   - Live chat with moderation
   - Emoji reactions
   - User voting on tracks

3. **Multi-Station Support**:
   - Station pooling for scaling
   - Load balancing algorithm
   - Failover mechanisms

4. **AI Integration**:
   - Automated playlist generation
   - Content moderation
   - Music recommendations

---

## Conclusion

This entity model provides a robust foundation for managing live streaming sessions powered by AzuraCast. The design follows DDD principles with clear aggregate roots, maintains separation of concerns through clean architecture, and ensures reliable real-time communication via SignalR and the transactional outbox pattern.
