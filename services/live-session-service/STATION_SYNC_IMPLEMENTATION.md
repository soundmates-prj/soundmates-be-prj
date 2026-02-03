# Station Sync Implementation - Summary

## Overview
Implemented automatic synchronization of AzuraCast stations to local database with full CRUD operations.

## Architecture

### Domain Layer
- **AzuraCastStation Entity**: Already existed with all necessary properties
- **AzuraCastStation.Methods.cs**: Factory methods and business logic for station management
- **IAzuraCastStationRepository**: Repository interface for station data access

### Infrastructure Layer
1. **AzuraCastStationRepository** (NEW)
   - Location: `LiveSessionService.Infrastructure/Repositories/AzuraCastStationRepository.cs`
   - Implements IAzuraCastStationRepository
   - Provides CRUD operations for stations
   - Methods:
     - `GetByIdAsync(Guid)` - Get station by internal ID
     - `GetByExternalIdAsync(int)` - Get station by AzuraCast external ID
     - `GetAllEnabledAsync()` - Get all enabled stations
     - `AddAsync()` - Create new station
     - `UpdateAsync()` - Update existing station
     - `DeleteAsync()` - Delete station

2. **DependencyInjection.cs** (UPDATED)
   - Added `IAzuraCastStationRepository` registration

### Application Layer
1. **Commands**
   - **SyncStationsCommand**: Command to trigger station synchronization
   - **SyncStationsHandler**: Handles the sync logic
     - Fetches stations from AzuraCast API
     - Creates new stations or updates existing ones based on ExternalStationId
     - Returns sync statistics (created, updated, failed)

2. **Queries**
   - **GetAllStationsQuery**: Query to get all stations from local database
   - **GetAllStationsHandler**: Returns list of stations from local database

3. **Results**
   - **SyncStationsResult**: Contains sync statistics and error messages
   - **StationResult**: DTO for station information

### API Layer
**StationController** (UPDATED)
Location: `LiveSessionService.Api/Controllers/StationController.cs`

#### Endpoints:

1. **GET /api/v1/stations/local**
   - Gets all stations from local database
   - Returns synced station data
   - Response: `List<StationResult>`

2. **GET /api/v1/stations**
   - Gets real-time stations from AzuraCast API
   - Does NOT save to database
   - Response: `List<AzuraCastStationListData>`

3. **POST /api/v1/stations/sync**
   - Syncs all stations from AzuraCast to local database
   - Creates new or updates existing stations
   - Response: `SyncStationsResult` with statistics
   - Returns counts for created, updated, and failed stations

4. **GET /api/v1/stations/{stationId:int}**
   - Gets specific station with now playing info from AzuraCast
   - Uses AzuraCast external station ID (1, 2, 3, etc.)
   - Response: `AzuraCastNowPlayingData`

## Database Schema

The `azuracast_stations` table stores synced stations with:
- `id` (Guid) - Internal primary key
- `external_station_id` (int) - AzuraCast station ID (1, 2, 3, 4...)
- `station_name` (string) - Station name
- `station_shortcode` (string?) - Short code
- `description` (string?) - Description
- `stream_url` (string) - Streaming URL
- `public_player_url` (string?) - Public player URL
- `is_enabled` (bool) - Whether station is enabled
- `sync_status` (enum) - NotSynced, Syncing, Synced, Failed
- `last_synced_at` (DateTime?) - Last sync timestamp
- `created_at` (DateTime) - Creation timestamp
- `updated_at` (DateTime?) - Last update timestamp

## Usage Flow

### 1. Initial Setup - Sync Stations
```http
POST /api/v1/stations/sync
```
Response:
```json
{
  "success": true,
  "data": {
    "totalStations": 3,
    "createdStations": 3,
    "updatedStations": 0,
    "failedStations": 0,
    "errors": []
  },
  "message": "Sync completed. Created: 3, Updated: 0, Failed: 0"
}
```

### 2. Get Synced Stations from Database
```http
GET /api/v1/stations/local
```
Response:
```json
{
  "success": true,
  "data": [
    {
      "id": "guid-here",
      "externalStationId": 1,
      "stationName": "Radio Station 1",
      "stationShortcode": "radio1",
      "description": "Description here",
      "streamUrl": "https://azuracast.com/radio/8000/radio.mp3",
      "publicPlayerUrl": "https://azuracast.com/public/radio1",
      "isEnabled": true,
      "lastSyncedAt": "2024-01-01T00:00:00Z",
      "syncStatus": "Synced"
    }
  ],
  "message": "Stations retrieved successfully from local database"
}
```

### 3. Get Real-time Station Data (without saving)
```http
GET /api/v1/stations
```
Fetches current data from AzuraCast API without persisting to database.

### 4. Get Specific Station with Now Playing
```http
GET /api/v1/stations/1
```
Gets station with ID 1 from AzuraCast with current now playing information.

## Key Features

1. **Automatic Sync**: POST /sync endpoint creates or updates stations automatically
2. **Upsert Logic**: Uses `ExternalStationId` to determine create vs update
3. **Error Handling**: Tracks failed stations and provides detailed error messages
4. **Sync Status**: Each station tracks its sync status and last sync time
5. **Clean Architecture**: Follows CQRS pattern with commands and queries
6. **Domain-Driven Design**: Business logic in domain entity methods

## Benefits

- **Data Persistence**: Stations are stored locally for faster access
- **Offline Capability**: Can work with cached station data
- **Consistency**: Ensures local database matches AzuraCast state
- **Audit Trail**: Tracks when stations were last synced
- **Flexibility**: Can manually trigger sync or automate it

## Future Enhancements

1. **Background Sync**: Add scheduled background job to auto-sync stations
2. **Webhook Support**: Listen to AzuraCast webhooks for real-time updates
3. **Selective Sync**: Sync only specific stations
4. **Soft Delete**: Mark stations as deleted instead of removing them
5. **Station Health Check**: Monitor station availability and status
