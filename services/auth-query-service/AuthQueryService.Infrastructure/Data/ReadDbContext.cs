using Microsoft.EntityFrameworkCore;
using AuthQueryService.Infrastructure.Data.Entities;

namespace AuthQueryService.Infrastructure.Data
{
    public sealed class ReadDbContext : DbContext
    {
        public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options) { }

        public DbSet<UserRead> Users => Set<UserRead>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReadDbContext).Assembly);
        }
    }
}