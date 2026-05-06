using AccountContentService.Domain.Entities;

namespace AccountContentService.Application.Interfaces.Repositories;

public interface IUserProfileReadModelRepository
{
    Task<UserProfileReadModel?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<UserProfileReadModel>> GetByIdsAsync(
    IEnumerable<Guid> userIds,
    CancellationToken cancellationToken);
    Task UpsertAsync(UserProfileReadModel profile, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, CancellationToken ct = default);
}
