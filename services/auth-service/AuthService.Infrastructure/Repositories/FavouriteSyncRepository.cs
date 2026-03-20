using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace AuthService.Infrastructure.Repositories;

/// <summary>
/// MongoDB dual-write implementation of <see cref="IFavouriteSyncRepository"/>.
///
/// Writes to the <c>user_favourites_read</c> collection in the same MongoDB
/// instance used by auth-query-service.
///
/// Design decisions:
/// ─────────────────
/// • IMongoDatabase is resolved lazily (the connection string may not be set
///   if MongoDB is not configured — this allows graceful degradation).
/// • ALL exceptions are caught and logged as warnings; they are NOT re-thrown.
///   The primary PostgreSQL write must always succeed even if MongoDB is down.
/// • The collection uses the same document shape as FavouriteReadModel in
///   auth-query-service, including snake_case field names.
/// </summary>
public sealed class FavouriteSyncRepository : IFavouriteSyncRepository
{
    private readonly IMongoCollection<BsonDocument>? _collection;
    private readonly ILogger<FavouriteSyncRepository> _logger;

    public FavouriteSyncRepository(
        IConfiguration configuration,
        ILogger<FavouriteSyncRepository> logger)
    {
        _logger = logger;

        try
        {
            // Register GUID serializer once (idempotent guard)
            try { BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard)); }
            catch { /* already registered */ }

            var connectionString =
                configuration["ConnectionStrings:MongoDb"]
                ?? configuration["MONGODB_CONNECTION_STRING"]
                ?? "mongodb://mongodb:27017";

            var databaseName =
                configuration["Mongo:Database"]
                ?? configuration["MONGODB_DATABASE"]
                ?? "auth_query";

            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<BsonDocument>("user_favourites_read");

            // Ensure compound unique index (mirrors auth-query-service FavouriteReadRepository)
            var indexKeys = Builders<BsonDocument>.IndexKeys
                .Ascending("user_id")
                .Ascending("item_type")
                .Ascending("item_id")
                .Ascending("source");

            _collection.Indexes.CreateOne(
                new CreateIndexModel<BsonDocument>(
                    indexKeys,
                    new CreateIndexOptions { Unique = true, Name = "ux_fav_user_item_source" }),
                new CreateOneIndexOptions());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "FavouriteSyncRepository: MongoDB connection failed — dual-write disabled. " +
                "Favourites will be synced via event replay.");
            _collection = null;
        }
    }

    // Helper: convert nullable string → BsonValue (BsonString or BsonNull)
    private static BsonValue NullableToBson(string? value)
        => value is not null ? (BsonValue)new BsonString(value) : BsonNull.Value;

    // Helper: convert nullable DateTime → BsonValue
    private static BsonValue NullableToBson(DateTime? value)
        => value.HasValue ? (BsonValue)new BsonDateTime(value.Value) : BsonNull.Value;

    public async Task SyncUpsertAsync(
        Guid id, Guid userId, string itemType, string itemId, string source,
        string? name, string? artistName, string? albumName,
        string? imgUrl, string? previewUrl, int? durationMs, string? externalUrl,
        DateTime? createdAt, CancellationToken ct = default)
    {
        if (_collection is null) return;

        try
        {
            // Use string GUIDs as _id and user_id for cross-service consistency
            // (auth-query-service FavouriteReadRepository also reads with string filter)
            var idStr     = id.ToString();
            var userIdStr = userId.ToString();

            var filter = Builders<BsonDocument>.Filter.Eq("_id", idStr);

            var doc = new BsonDocument
            {
                ["_id"]         = (BsonValue)new BsonString(idStr),
                ["user_id"]     = (BsonValue)new BsonString(userIdStr),
                ["item_type"]   = (BsonValue)new BsonString(itemType),
                ["item_id"]     = (BsonValue)new BsonString(itemId),
                ["source"]      = (BsonValue)new BsonString(source),
                ["name"]        = NullableToBson(name),
                ["artist_name"] = NullableToBson(artistName),
                ["album_name"]  = NullableToBson(albumName),
                ["img_url"]     = NullableToBson(imgUrl),
                ["preview_url"] = NullableToBson(previewUrl),
                ["duration_ms"] = durationMs.HasValue ? (BsonValue)new BsonInt32(durationMs.Value) : BsonNull.Value,
                ["external_url"]= NullableToBson(externalUrl),
                ["created_at"]  = NullableToBson(createdAt),
                ["updated_at"]  = (BsonValue)new BsonDateTime(DateTime.UtcNow)
            };

            await _collection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = true }, ct);

            _logger.LogDebug("FavouriteSyncRepository: upserted {Id} for user {UserId}.", id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "FavouriteSyncRepository: failed to upsert favourite {Id} for user {UserId}. " +
                "MongoDB sync skipped — primary write succeeded.", id, userId);
        }
    }

    public async Task SyncDeleteAsync(
        Guid userId, string itemType, string itemId, string source, CancellationToken ct = default)
    {
        if (_collection is null) return;

        try
        {
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("user_id", userId.ToString()),
                Builders<BsonDocument>.Filter.Eq("item_type", itemType),
                Builders<BsonDocument>.Filter.Eq("item_id", itemId),
                Builders<BsonDocument>.Filter.Eq("source", source));

            await _collection.DeleteOneAsync(filter, ct);

            _logger.LogDebug("FavouriteSyncRepository: deleted [{Type}/{Id}/{Source}] for user {UserId}.",
                itemType, itemId, source, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "FavouriteSyncRepository: failed to delete favourite for user {UserId}. " +
                "MongoDB sync skipped — primary delete succeeded.",
                userId);
        }
    }

    public async Task SyncUpdateAsync(
        Guid favouriteId,
        string? name,
        string? artistName,
        string? albumName,
        string? imgUrl,
        string? previewUrl,
        int? durationMs,
        string? externalUrl,
        DateTime updatedAt,
        CancellationToken ct = default)
    {
        if (_collection is null) return;

        try
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", favouriteId.ToString());

            // Build $set patch — only include fields that have a value
            // This preserves fields the caller didn't explicitly provide (true patch semantics)
            var setDoc = new BsonDocument { ["updated_at"] = new BsonDateTime(updatedAt) };

            if (name        is not null) setDoc["name"]         = new BsonString(name);
            if (artistName  is not null) setDoc["artist_name"]  = new BsonString(artistName);
            if (albumName   is not null) setDoc["album_name"]   = new BsonString(albumName);
            if (imgUrl      is not null) setDoc["img_url"]      = new BsonString(imgUrl);
            if (previewUrl  is not null) setDoc["preview_url"]  = new BsonString(previewUrl);
            if (durationMs  is not null) setDoc["duration_ms"]  = new BsonInt32(durationMs.Value);
            if (externalUrl is not null) setDoc["external_url"] = new BsonString(externalUrl);

            var update = new BsonDocument { ["$set"] = setDoc };
            await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);

            _logger.LogDebug("FavouriteSyncRepository: updated metadata for favourite {Id}.", favouriteId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "FavouriteSyncRepository: failed to update metadata for favourite {Id}. " +
                "MongoDB sync skipped — primary update succeeded.", favouriteId);
        }
    }
}
