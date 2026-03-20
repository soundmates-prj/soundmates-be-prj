using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AuthQueryService.Infrastructure.Repositories
{
    /// <summary>
    /// MongoDB implementation of <see cref="IFavouriteReadRepository"/>.
    ///
    /// Collection: <c>user_favourites_read</c>
    ///
    /// Uses raw <see cref="BsonDocument"/> for reads/writes so we avoid ClassMap
    /// ordering issues and can always control the exact field names written by
    /// auth-service's FavouriteSyncRepository (snake_case).
    ///
    /// Indexes:
    ///   - (user_id) — primary user filter
    ///   - (user_id, item_type) — filtered list queries
    ///   - (user_id, item_type, item_id, source) unique — mirrors PG constraint
    /// </summary>
    public sealed class FavouriteReadRepository : IFavouriteReadRepository
    {
        private readonly IMongoCollection<BsonDocument> _collection;

        private static bool _indexesEnsured;
        private static readonly object _indexLock = new();

        public FavouriteReadRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<BsonDocument>("user_favourites_read");
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            if (_indexesEnsured) return;

            lock (_indexLock)
            {
                if (_indexesEnsured) return;

                var indexModels = new CreateIndexModel<BsonDocument>[]
                {
                    new(Builders<BsonDocument>.IndexKeys.Ascending("user_id"),
                        new CreateIndexOptions { Name = "ix_fav_user_id" }),

                    new(Builders<BsonDocument>.IndexKeys
                        .Ascending("user_id").Ascending("item_type"),
                        new CreateIndexOptions { Name = "ix_fav_user_type" }),

                    new(Builders<BsonDocument>.IndexKeys
                        .Ascending("user_id").Ascending("item_type")
                        .Ascending("item_id").Ascending("source"),
                        new CreateIndexOptions { Unique = true, Name = "ux_fav_user_item_source" })
                };

                try { _collection.Indexes.CreateMany(indexModels); }
                catch { /* already exist */ }

                _indexesEnsured = true;
            }
        }

        // ── Reads ──────────────────────────────────────────────────────────────

        public async Task<List<FavouriteReadModel>> GetByUserIdAsync(
            Guid userId,
            string? itemType = null,
            string? source = null,
            CancellationToken ct = default)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", userId.ToString());

            if (!string.IsNullOrWhiteSpace(itemType))
                filter &= Builders<BsonDocument>.Filter.Eq("item_type", itemType.ToLowerInvariant());

            if (!string.IsNullOrWhiteSpace(source))
                filter &= Builders<BsonDocument>.Filter.Eq("source", source.ToLowerInvariant());

            var docs = await _collection
                .Find(filter)
                .SortByDescending(d => d["created_at"])
                .ToListAsync(ct);

            return docs.Select(ToModel).ToList();
        }

        public async Task<bool> ExistsAsync(
            Guid userId, string itemType, string itemId, string source, CancellationToken ct = default)
        {
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("user_id", userId.ToString()),
                Builders<BsonDocument>.Filter.Eq("item_type", itemType),
                Builders<BsonDocument>.Filter.Eq("item_id", itemId),
                Builders<BsonDocument>.Filter.Eq("source", source));

            return await _collection.CountDocumentsAsync(filter, cancellationToken: ct) > 0;
        }

        // ── Writes (projection events) ─────────────────────────────────────────

        public async Task UpsertAsync(FavouriteReadModel model, CancellationToken ct = default)
        {
            var doc = ToDocument(model);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]);
            await _collection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = true }, ct);
        }

        public async Task DeleteAsync(
            Guid userId, string itemType, string itemId, string source, CancellationToken ct = default)
        {
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("user_id", userId.ToString()),
                Builders<BsonDocument>.Filter.Eq("item_type", itemType),
                Builders<BsonDocument>.Filter.Eq("item_id", itemId),
                Builders<BsonDocument>.Filter.Eq("source", source));

            await _collection.DeleteOneAsync(filter, ct);
        }

        public async Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", userId.ToString());
            await _collection.DeleteManyAsync(filter, ct);
        }

        // ── Mapping helpers ────────────────────────────────────────────────────

        private static FavouriteReadModel ToModel(BsonDocument d) => new()
        {
            Id          = d.TryGetValue("_id", out var idVal) ? ParseGuid(idVal) : Guid.Empty,
            UserId      = d.TryGetValue("user_id", out var uid) ? ParseGuid(uid) : Guid.Empty,
            ItemType    = d.GetString("item_type"),
            ItemId      = d.GetString("item_id"),
            Source      = d.GetString("source"),
            Name        = d.GetNullableString("name"),
            ArtistName  = d.GetNullableString("artist_name"),
            AlbumName   = d.GetNullableString("album_name"),
            ImgUrl      = d.GetNullableString("img_url"),
            PreviewUrl  = d.GetNullableString("preview_url"),
            DurationMs  = d.TryGetValue("duration_ms", out var dur) && !dur.IsBsonNull ? (int?)dur.AsInt32 : null,
            ExternalUrl = d.GetNullableString("external_url"),
            CreatedAt   = d.TryGetValue("created_at", out var ca) && !ca.IsBsonNull
                            ? (DateTime?)ca.AsBsonDateTime.ToUniversalTime() : null,
            UpdatedAt   = d.TryGetValue("updated_at", out var ua) && !ua.IsBsonNull
                            ? (DateTime?)ua.AsBsonDateTime.ToUniversalTime() : null,
        };

        private static BsonDocument ToDocument(FavouriteReadModel m) => new()
        {
            ["_id"]         = (BsonValue)new BsonString(m.Id.ToString()),
            ["user_id"]     = (BsonValue)new BsonString(m.UserId.ToString()),
            ["item_type"]   = (BsonValue)new BsonString(m.ItemType),
            ["item_id"]     = (BsonValue)new BsonString(m.ItemId),
            ["source"]      = (BsonValue)new BsonString(m.Source),
            ["name"]        = m.Name is not null ? (BsonValue)new BsonString(m.Name) : BsonNull.Value,
            ["artist_name"] = m.ArtistName is not null ? (BsonValue)new BsonString(m.ArtistName) : BsonNull.Value,
            ["album_name"]  = m.AlbumName is not null ? (BsonValue)new BsonString(m.AlbumName) : BsonNull.Value,
            ["img_url"]     = m.ImgUrl is not null ? (BsonValue)new BsonString(m.ImgUrl) : BsonNull.Value,
            ["preview_url"] = m.PreviewUrl is not null ? (BsonValue)new BsonString(m.PreviewUrl) : BsonNull.Value,
            ["duration_ms"] = m.DurationMs.HasValue ? (BsonValue)new BsonInt32(m.DurationMs.Value) : BsonNull.Value,
            ["external_url"]= m.ExternalUrl is not null ? (BsonValue)new BsonString(m.ExternalUrl) : BsonNull.Value,
            ["created_at"]  = m.CreatedAt.HasValue ? (BsonValue)new BsonDateTime(m.CreatedAt.Value) : BsonNull.Value,
            ["updated_at"]  = m.UpdatedAt.HasValue ? (BsonValue)new BsonDateTime(m.UpdatedAt.Value) : BsonNull.Value,
        };

        private static Guid ParseGuid(BsonValue v)
            => Guid.TryParse(v.IsString ? v.AsString : v.ToString(), out var g) ? g : Guid.Empty;
    }

    // ── Extension helpers for cleaner BsonDocument reads ──────────────────────
    internal static class BsonDocumentExtensions
    {
        public static string GetString(this BsonDocument d, string key)
            => d.TryGetValue(key, out var v) && !v.IsBsonNull ? v.AsString : string.Empty;

        public static string? GetNullableString(this BsonDocument d, string key)
            => d.TryGetValue(key, out var v) && !v.IsBsonNull ? v.AsString : null;
    }
}
