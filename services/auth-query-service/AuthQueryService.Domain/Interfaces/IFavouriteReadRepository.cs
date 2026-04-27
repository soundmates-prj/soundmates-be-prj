using AuthQueryService.Domain.Entities.ReadModels;

namespace AuthQueryService.Domain.Interfaces
{
    /// <summary>
    /// Read-side repository for user favourites stored in MongoDB.
    /// All methods are read-only (query side of CQRS).
    /// Write side lives in auth-service (PostgreSQL).
    /// </summary>
    public interface IFavouriteReadRepository
    {
        /// <summary>
        /// Get all favourites for a user, with optional filtering by itemType and/or source.
        /// </summary>
        Task<List<FavouriteReadModel>> GetByUserIdAsync(
            Guid userId,
            string? itemType = null,
            string? source = null,
            CancellationToken ct = default);

        /// <summary>
        /// Check if a specific favourite record exists (used for existence checks).
        /// </summary>
        Task<bool> ExistsAsync(Guid userId, string itemType, string itemId, string source, CancellationToken ct = default);

        // ── Write methods (called by projection service when events arrive from RabbitMQ) ──

        Task UpsertAsync(FavouriteReadModel model, CancellationToken ct = default);

        Task DeleteAsync(Guid userId, string itemType, string itemId, string source, CancellationToken ct = default);

        Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default);
    }
}
