using AuthQueryService.Domain.Entities;
using AuthQueryService.Domain.Interfaces;
using MongoDB.Driver;

namespace AuthQueryService.Infrastructure.Repositories
{
    public sealed class RoleRepository : IRoleRepository
    {
        private readonly IMongoCollection<UserRole> _collection;

        public RoleRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<UserRole>("roles_read");
        }

        public async Task<UserRole?> GetByIdAsync(Guid id)
            => await _collection.Find(r => r.Id == id).FirstOrDefaultAsync();

        public async Task<UserRole?> GetByNameAsync(string name)
            => await _collection.Find(r => r.Name == name).FirstOrDefaultAsync();

        public async Task<List<UserRole>> GetAllAsync()
            => await _collection.Find(FilterDefinition<UserRole>.Empty).ToListAsync();
    }
}

