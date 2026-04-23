using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Persistence.Repositories;

public class BankAccountRepository : IBankAccountRepository
{
    private readonly AuthDbContext _context;

    public BankAccountRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<BankAccount?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.BankAccounts.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
    {
        await _context.BankAccounts.AddAsync(bankAccount, cancellationToken);
    }

    public Task UpdateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
    {
        _context.BankAccounts.Update(bankAccount);
        return Task.CompletedTask;
    }
}
