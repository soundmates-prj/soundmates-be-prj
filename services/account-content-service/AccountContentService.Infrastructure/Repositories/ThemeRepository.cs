using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AccountContentService.Infrastructure.Repositories
{
    public class ThemeRepository : IThemeRepository
    {
        private readonly AccountContentDbContext _dbContext;

        public ThemeRepository(AccountContentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Theme theme, CancellationToken cancellationToken)
        {
            await _dbContext.Themes.AddAsync(theme, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var theme = await _dbContext.Themes
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (theme == null) return;

            _dbContext.Themes.Remove(theme);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
        {
            return await _dbContext.Themes
                .AsNoTracking()
                .AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);
        }

        public async Task<PaginationResult<Theme>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = _dbContext.Themes
                .AsNoTracking()
                .Where(x => x.IsActive == true)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .OrderByDescending(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<Theme>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<Theme?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _dbContext.Themes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<PaginationResult<Theme>> GetAllByNameAsync(string name, int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = _dbContext.Themes
                .AsNoTracking()
                .Where(x => x.Name.ToLower() == name.ToLower() && x.IsActive == true)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .OrderByDescending(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<Theme>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PaginationResult<Theme>> GetActiveAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = _dbContext.Themes
                .AsNoTracking()
                .Where(x => x.IsActive == true)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .OrderByDescending(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<Theme>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task UpdateAsync(Theme theme, CancellationToken cancellationToken)
        {
            _dbContext.Update(theme);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}