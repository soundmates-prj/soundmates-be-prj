namespace AuthQueryService.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for a user favourite, returned from GET /me/favorites.
    /// Fields are pre-joined from the denormalized MongoDB document.
    /// </summary>
    public sealed class UserFavouriteDto
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }

        /// <summary>track | artist | album | playlist | podcast | episode | show | blog | schedule</summary>
        public string ItemType { get; init; } = null!;

        /// <summary>Spotify ID, internal GUID (as string), or other external reference</summary>
        public string ItemId { get; init; } = null!;

        /// <summary>spotify | local | other</summary>
        public string Source { get; init; } = null!;

        // ── Display metadata (may be null for internal items not yet enriched) ──
        public string? Name { get; init; }
        public string? ArtistName { get; init; }
        public string? AlbumName { get; init; }
        public string? ImgUrl { get; init; }
        public string? PreviewUrl { get; init; }
        public int? DurationMs { get; init; }
        public string? ExternalUrl { get; init; }

        public DateTime? CreatedAt { get; init; }
    }
}
