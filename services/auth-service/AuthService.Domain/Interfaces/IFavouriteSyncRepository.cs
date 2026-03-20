namespace AuthService.Domain.Interfaces;

/// <summary>
/// Synchronizes Favourite writes to the read-side MongoDB (auth-query-service).
///
/// Pattern: Dual-Write
/// ───────────────────
/// When a user adds/removes/updates a favourite in auth-service (PostgreSQL write side),
/// we ALSO write the same data to MongoDB so auth-query-service (read side)
/// always has an up-to-date, denormalized document — no need for event-driven sync.
///
/// Why here (Domain Interface, Infrastructure Implementation)?
/// - Keeps Application handlers decoupled from MongoDB specifics.
/// - Makes it easy to replace with an async queue later if needed.
/// - Failure to sync is intentionally non-fatal: we log a warning and continue
///   so a MongoDB hiccup doesn't break the primary write to PostgreSQL.
/// </summary>
public interface IFavouriteSyncRepository
{
    /// <summary>
    /// Upsert a favourite document in the MongoDB read collection.
    /// Called after PostgreSQL SaveChanges succeeds (Add).
    /// </summary>
    Task SyncUpsertAsync(
        Guid id,
        Guid userId,
        string itemType,
        string itemId,
        string source,
        string? name,
        string? artistName,
        string? albumName,
        string? imgUrl,
        string? previewUrl,
        int? durationMs,
        string? externalUrl,
        DateTime? createdAt,
        CancellationToken ct = default);

    /// <summary>
    /// Partially update metadata fields of an existing favourite document.
    /// Only non-null values in the provided arguments are updated (patch semantics).
    /// Called after PostgreSQL SaveChanges succeeds (Update).
    /// </summary>
    Task SyncUpdateAsync(
        Guid favouriteId,
        string? name,
        string? artistName,
        string? albumName,
        string? imgUrl,
        string? previewUrl,
        int? durationMs,
        string? externalUrl,
        DateTime updatedAt,
        CancellationToken ct = default);

    /// <summary>
    /// Delete a favourite document from the MongoDB read collection.
    /// Called after PostgreSQL SaveChanges succeeds (Delete).
    /// </summary>
    Task SyncDeleteAsync(
        Guid userId,
        string itemType,
        string itemId,
        string source,
        CancellationToken ct = default);
}
