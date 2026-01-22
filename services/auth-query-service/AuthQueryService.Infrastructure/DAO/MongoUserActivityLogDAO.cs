using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Infrastructure.DAO.Interfaces;
using MongoDB.Driver;

namespace AuthQueryService.Infrastructure.DAO
{
    public sealed class MongoUserActivityLogDAO : IUserActivityLogDAO
    {
        private readonly IMongoCollection<UserActivityLog> _col;

        public MongoUserActivityLogDAO(IMongoDatabase db)
        {
            _col = db.GetCollection<UserActivityLog>("user_activity_logs");
            
            // Create indexes for better query performance
            var indexModels = new[]
            {
                new CreateIndexModel<UserActivityLog>(
                    Builders<UserActivityLog>.IndexKeys.Ascending(x => x.UserId),
                    new CreateIndexOptions { Name = "ix_user_activity_logs_user_id" }),
                new CreateIndexModel<UserActivityLog>(
                    Builders<UserActivityLog>.IndexKeys.Ascending(x => x.ActivityType),
                    new CreateIndexOptions { Name = "ix_user_activity_logs_activity_type" }),
                new CreateIndexModel<UserActivityLog>(
                    Builders<UserActivityLog>.IndexKeys.Descending(x => x.OccurredAt),
                    new CreateIndexOptions { Name = "ix_user_activity_logs_occurred_at" }),
                new CreateIndexModel<UserActivityLog>(
                    Builders<UserActivityLog>.IndexKeys.Ascending(x => x.Email),
                    new CreateIndexOptions { Name = "ix_user_activity_logs_email" }),
                new CreateIndexModel<UserActivityLog>(
                    Builders<UserActivityLog>.IndexKeys.Ascending(x => x.Username),
                    new CreateIndexOptions { Name = "ix_user_activity_logs_username" })
            };
            
            try
            {
                _col.Indexes.CreateMany(indexModels);
            }
            catch
            {
                // Indexes might already exist, ignore
            }
        }

        public async Task CreateAsync(UserActivityLog log)
        {
            await _col.InsertOneAsync(log);
        }

        public async Task<UserActivityLog?> GetByIdAsync(Guid id)
        {
            return await _col.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<UserActivityLog>> GetByUserIdAsync(Guid userId, int limit = 50)
        {
            return await _col.Find(x => x.UserId == userId)
                .SortByDescending(x => x.OccurredAt)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task<List<UserActivityLog>> GetRecentActivitiesAsync(int limit = 100)
        {
            return await _col.Find(FilterDefinition<UserActivityLog>.Empty)
                .SortByDescending(x => x.OccurredAt)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task<List<UserActivityLog>> GetFailedLoginAttemptsAsync(string? emailOrUsername = null, int hours = 24)
        {
            var builder = Builders<UserActivityLog>.Filter;
            var cutoffTime = DateTime.UtcNow.AddHours(-hours);
            
            var filter = builder.And(
                builder.Eq(x => x.IsSuccess, false),
                builder.In(x => x.ActivityType, new[] { "login", "google_login" }),
                builder.Gte(x => x.OccurredAt, cutoffTime)
            );

            if (!string.IsNullOrWhiteSpace(emailOrUsername))
            {
                filter = builder.And(filter, builder.Or(
                    builder.Eq(x => x.Email, emailOrUsername),
                    builder.Eq(x => x.Username, emailOrUsername)
                ));
            }

            return await _col.Find(filter)
                .SortByDescending(x => x.OccurredAt)
                .ToListAsync();
        }

        public async Task<(List<UserActivityLog> Items, int TotalCount)> GetPagedAsync(
            int page, 
            int size, 
            string? activityType = null, 
            bool? isSuccess = null)
        {
            page = page <= 0 ? 1 : page;
            size = size <= 0 ? 20 : size;

            var builder = Builders<UserActivityLog>.Filter;
            var filter = FilterDefinition<UserActivityLog>.Empty;

            if (!string.IsNullOrWhiteSpace(activityType))
            {
                filter = builder.And(filter, builder.Eq(x => x.ActivityType, activityType));
            }

            if (isSuccess.HasValue)
            {
                filter = builder.And(filter, builder.Eq(x => x.IsSuccess, isSuccess.Value));
            }

            var total = (int)await _col.CountDocumentsAsync(filter);
            var items = await _col.Find(filter)
                .SortByDescending(x => x.OccurredAt)
                .Skip((page - 1) * size)
                .Limit(size)
                .ToListAsync();

            return (items, total);
        }
    }
}
