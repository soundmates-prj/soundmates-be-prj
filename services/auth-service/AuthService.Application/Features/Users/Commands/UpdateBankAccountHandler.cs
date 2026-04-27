using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Users.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Users.Handlers;

public class UpdateBankAccountHandler : ICommandHandler<UpdateBankAccountCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBankAccountHandler(
        IUserRepository userRepository, 
        IBankAccountRepository bankAccountRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _bankAccountRepository = bankAccountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (user == null)
        {
            return Result<bool>.Failure("User not found", 404);
        }

        var bankAccount = await _bankAccountRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (bankAccount == null)
        {
            bankAccount = new BankAccount
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                BankId = request.BankId,
                AccountNumber = request.AccountNumber,
                AccountName = request.AccountName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _bankAccountRepository.AddAsync(bankAccount, cancellationToken);
        }
        else
        {
            bankAccount.BankId = request.BankId;
            bankAccount.AccountNumber = request.AccountNumber;
            bankAccount.AccountName = request.AccountName;
            bankAccount.UpdatedAt = DateTime.UtcNow;
            await _bankAccountRepository.UpdateAsync(bankAccount, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
