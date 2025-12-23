using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Domain.Interfaces
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        DbContext Context { get; }
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task BeginTransactionAsync(CancellationToken ct = default);
        Task CommitAsync(CancellationToken ct = default);
        Task RollbackAsync(CancellationToken ct = default);
    }
}