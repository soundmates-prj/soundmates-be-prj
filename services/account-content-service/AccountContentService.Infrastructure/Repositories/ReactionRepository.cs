using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Repositories
{
    public class ReactionRepository :IPostReactionRepository
    {
       
            private readonly AccountContentDbContext _context;

            public ReactionRepository(AccountContentDbContext context)
            {
                _context = context;
            }

            public async Task AddAsync(PostReaction comment)
            {
                await _context.PostReactions.AddAsync(comment);
                await _context.SaveChangesAsync();
            }

            public async Task DeleteAsync(PostReaction comment)
            {
                _context.PostReactions.Remove(comment);
                await _context.SaveChangesAsync();
            }

            public async Task<PostReaction> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            {
                return await _context.PostReactions
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .FirstOrDefaultAsync(cancellationToken);
            }



            public async Task<PaginationResult<PostReaction>> GetByUserIdAsync(
                     Guid userId,
                    int pageSize, int page,
                    CancellationToken cancellationToken)
            {
                var query = _context.PostReactions
                    .AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .AsQueryable();

                var totalCount = await query.CountAsync(cancellationToken);

                var posts = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                return new PaginationResult<PostReaction>
                {
                    Items = posts,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }

            public async Task<List<PostReaction>> GetDetailByIdAsync(
                     Guid id,
                    CancellationToken cancellationToken)
            {
                var query = _context.PostReactions
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .AsQueryable();

                return await query
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync(cancellationToken);
            }

            public async Task<PaginationResult<PostReaction>> GetByPostIdAsync(Guid postId,
                int pageSize, int page, CancellationToken cancellationToken)
            {
                var query = _context.PostReactions
                    .AsNoTracking()
                    .Where(x => x.PostId == postId)
                    .AsQueryable();

                var totalCount = await query.CountAsync(cancellationToken);

                var posts = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                return new PaginationResult<PostReaction>
                {
                    Items = posts,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }

            public async Task UpdateAsync(PostReaction comment)
            {
                _context.PostReactions.Update(comment);
                await _context.SaveChangesAsync();
            }
        }
    }

