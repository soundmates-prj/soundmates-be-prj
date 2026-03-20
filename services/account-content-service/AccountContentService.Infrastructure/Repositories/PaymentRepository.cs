using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AccountContentDbContext _context;


        public PaymentRepository(AccountContentDbContext context)
        {
            _context = context;
        }

        // =========================
        // CREATE
        // =========================
        public async Task AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
        }

        // =========================
        // UPDATE
        // =========================
        public async Task UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
        }

        // =========================
        // DELETE
        // =========================
        public async Task DeleteAsync(Payment payment)
        {
            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
        }

        public async Task<Payment> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Payments
              .AsNoTracking()
              .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
    }
}
