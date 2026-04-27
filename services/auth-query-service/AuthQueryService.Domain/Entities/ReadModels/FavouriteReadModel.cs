namespace AuthQueryService.Domain.Entities.ReadModels
{
    /// <summary>
    /// MongoDB Read Model for user favourites.
    ///
    /// Stored in the <c>user_favourites_read</c> collection in MongoDB.
    ///
    /// Design notes:
    ///   - PURE domain object — no MongoDB attributes here. Column name mapping
    ///     is handled in the Infrastructure layer via ClassMap or BsonSerializer conventions.
    ///   - Denormalized: metadata (Name, ImgUrl, etc.) is embedded to avoid JOIN lookups.
    ///   - Projection service (event consumer) writes to this collection when
    ///     auth-service publishes FavouriteCreated / FavouriteDeleted events.
    /// </summary>
    public sealed class FavouriteReadModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        /// <summary>track | artist | album | playlist | podcast | episode | show | blog | schedule</summary>
        public string ItemType { get; set; } = string.Empty;

        /// <summary>Spotify ID, internal GUID, or external reference</summary>
        public string ItemId { get; set; } = string.Empty;

        /// <summary>spotify | local | other</summary>
        public string Source { get; set; } = string.Empty;

        // ── Denormalized display metadata ──
        public string? Name { get; set; }
        public string? ArtistName { get; set; }
        public string? AlbumName { get; set; }
        public string? ImgUrl { get; set; }
        public string? PreviewUrl { get; set; }
        public int? DurationMs { get; set; }
        public string? ExternalUrl { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
