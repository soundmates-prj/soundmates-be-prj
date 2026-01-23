using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using MongoDB.Driver;

namespace AuthQueryService.Infrastructure.Repositories
{
    /// <summary>
    /// MongoDB implementation of IRoleRepository for read-only role queries
    /// </summary>
    public sealed class RoleRepository : IRoleRepository
    {
        private readonly IMongoCollection<RoleReadModel> _collection;

        public RoleRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<RoleReadModel>("roles_read");
        }

        public async Task<RoleReadModel?> GetByIdAsync(Guid id)
            => await _collection.Find(r => r.Id == id).FirstOrDefaultAsync();

        public async Task<RoleReadModel?> GetByNameAsync(string name)
            => await _collection.Find(r => r.Name == name).FirstOrDefaultAsync();

        public async Task<List<RoleReadModel>> GetAllAsync()
            => await _collection.Find(FilterDefinition<RoleReadModel>.Empty).ToListAsync();
    }
}

