using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Repositories
{
    public class SystemSettingReposiotry : ISystemSettingReposiotry
    {
        private readonly AccountContentDbContext _context;

        public SystemSettingReposiotry(AccountContentDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(SystemSetting setting)
        {
            await _context.SystemSettings.AddAsync(setting);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(SystemSetting setting)
        {
            _context.SystemSettings.Remove(setting);
            await _context.SaveChangesAsync();
        }

        public async Task<PaginationResult<SystemSetting>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = _context.SystemSettings
                .AsNoTracking()
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<SystemSetting>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<SystemSetting> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.SystemSettings
                .AsNoTracking()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<SystemSetting> GetByKeyAsync(string key, CancellationToken cancellationToken)
        {
            return await _context.SystemSettings
                 .AsNoTracking()
                 .Where(x => x.Key.ToLower() == key.ToLower())
                 .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(SystemSetting setting)
        {
            _context.SystemSettings.Update(setting);
            await _context.SaveChangesAsync();
        }
    }
}
