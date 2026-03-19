using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
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

        public async Task UpdateAsync(PaymentTransaction paymentTransaction)
        {
            _context.Update(paymentTransaction);
            await _context.SaveChangesAsync();
        }
    }
}
