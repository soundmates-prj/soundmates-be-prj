using System;
using System.Threading;
using System.Threading.Tasks;

namespace AccountContentService.Application.Interfaces.Services;

public interface IAuthApiClient
{
    Task<BankAccountDto?> GetUserBankAccountAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<Guid>> GetAdminUserIdsAsync(CancellationToken cancellationToken);
}

public class BankAccountDto
{
    public Guid UserId { get; set; }
    public string BankId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
}
