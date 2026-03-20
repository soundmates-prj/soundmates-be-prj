using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Repositories
{
    public class PaymentTransactionRepository : IPaymentTransactionRepository
    {
        private readonly AccountContentDbContext _context;

        public PaymentTransactionRepository(AccountContentDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PaymentTransaction paymentTransaction)
        {
            await _context.PaymentTransactions.AddAsync(paymentTransaction);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(PaymentTransaction paymentTransaction)
        {
            _context.Remove(paymentTransaction);
            await _context.SaveChangesAsync();
        }

        public async Task<PaginationResult<PaymentTransaction>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = _context.PaymentTransactions
                .AsNoTracking()
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<PaymentTransaction>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PaymentTransaction> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.PaymentTransactions
                .AsNoTracking()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<PaginationResult<PaymentTransaction>> GetByUserId(Guid userId, int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = _context.PaymentTransactions
                .AsNoTracking()
                .Include(t => t.Payment)
                .Where(t => t.Payment.UserId == userId && t.TransactionStatus.ToLower().Equals(TransactionStatus.Success.ToString().ToLower()))
                .AsQueryable();
            

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<PaymentTransaction>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task UpdateAsync(PaymentTransaction paymentTransaction)
        {
            _context.Update(paymentTransaction);
            await _context.SaveChangesAsync();
        }
    }
}
