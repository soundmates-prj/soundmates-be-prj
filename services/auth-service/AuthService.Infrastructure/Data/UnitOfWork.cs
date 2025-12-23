using System.Threading;
using System.Threading.Tasks;
using AuthService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthService.Infrastructure.Data
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly AuthDbContext _dbContext;
        private IDbContextTransaction? _currentTx;

        public UnitOfWork(AuthDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public DbContext Context => _dbContext;

        public async Task BeginTransactionAsync(CancellationToken ct = default)
        {
            if (_currentTx != null) return;
            _currentTx = await _dbContext.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            if (_currentTx == null) return;

            await _dbContext.SaveChangesAsync(ct);
            await _currentTx.CommitAsync(ct);
            await _currentTx.DisposeAsync();
            _currentTx = null;
        }

        public async Task RollbackAsync(CancellationToken ct = default)
        {
            if (_currentTx == null) return;

            await _currentTx.RollbackAsync(ct);
            await _currentTx.DisposeAsync();
            _currentTx = null;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _dbContext.SaveChangesAsync(ct);

        public async ValueTask DisposeAsync()
        {
            if (_currentTx != null)
            {
                await _currentTx.DisposeAsync();
                _currentTx = null;
            }
        }
    }
}