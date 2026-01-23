using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using MongoDB.Driver;

namespace AuthQueryService.Infrastructure.Repositories
{
    /// <summary>
    /// MongoDB implementation of IUserReadRepository
    /// Directly connects to MongoDB without additional DAO layer
    /// </summary>
    public sealed class UserReadRepository : IUserReadRepository
    {
        private readonly IMongoCollection<UserReadModel> _collection;

        public UserReadRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<UserReadModel>("users_read");
            
            // Ensure indexes for better query performance
            var indexModels = new[]
            {
                new CreateIndexModel<UserReadModel>(
                    Builders<UserReadModel>.IndexKeys.Ascending(x => x.Username),
                    new CreateIndexOptions { Unique = true, Name = "ux_users_read_username" }),
                new CreateIndexModel<UserReadModel>(
                    Builders<UserReadModel>.IndexKeys.Ascending(x => x.Email),
                    new CreateIndexOptions { Unique = true, Name = "ux_users_read_email" })
            };
            
            try
            {
                _collection.Indexes.CreateMany(indexModels);
            }
            catch
            {
                // Indexes might already exist, ignore
            }
        }

        public async Task<UserReadModel?> GetByIdAsync(Guid id)
            => await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<UserReadModel?> GetByUsernameAsync(string username)
            => await _collection.Find(x => x.Username == username).FirstOrDefaultAsync();

        public async Task<List<UserReadModel>> GetAllAsync()
            => await _collection.Find(FilterDefinition<UserReadModel>.Empty).ToListAsync();

        public async Task<(List<UserReadModel> Items, int TotalCount)> GetPagedAsync(int page, int size)
        {
            page = page <= 0 ? 1 : page;
            size = size <= 0 ? 20 : size;

            var filter = FilterDefinition<UserReadModel>.Empty;
            var total = (int)await _collection.CountDocumentsAsync(filter);
            var items = await _collection.Find(filter)
                .SortBy(x => x.Username)
                .Skip((page - 1) * size)
                .Limit(size)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(List<UserReadModel> Items, int TotalCount)> SearchAsync(
            string? searchTerm, 
            int page, 
            int size)
        {
            page = page <= 0 ? 1 : page;
            size = size <= 0 ? 20 : size;

            FilterDefinition<UserReadModel> filter = FilterDefinition<UserReadModel>.Empty;
            
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var builder = Builders<UserReadModel>.Filter;
                filter = builder.Or(
                    builder.Regex(x => x.Username, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                    builder.Regex(x => x.Email, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                    builder.Regex(x => x.FirstName ?? "", new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                    builder.Regex(x => x.LastName ?? "", new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
                );
            }

            var total = (int)await _collection.CountDocumentsAsync(filter);
            var items = await _collection.Find(filter)
                .SortBy(x => x.Username)
                .Skip((page - 1) * size)
                .Limit(size)
                .ToListAsync();

            return (items, total);
        }

        public async Task UpsertAsync(UserReadModel model)
        {
            var filter = Builders<UserReadModel>.Filter.Eq(x => x.Id, model.Id);
            await _collection.ReplaceOneAsync(
                filter, 
                model, 
                new ReplaceOptions { IsUpsert = true });
        }
        
        public async Task DeleteAsync(Guid id)
            => await _collection.DeleteOneAsync(x => x.Id == id);
    }
}

