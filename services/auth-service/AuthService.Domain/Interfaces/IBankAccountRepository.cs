using AuthService.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Domain.Interfaces;

public interface IBankAccountRepository
{
    Task<BankAccount?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(BankAccount bankAccount, CancellationToken cancellationToken = default);
    Task UpdateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default);
}
