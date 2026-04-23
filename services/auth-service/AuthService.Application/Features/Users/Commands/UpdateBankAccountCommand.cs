using AuthService.Application.Abstractions.Messaging;
using System;

namespace AuthService.Application.Features.Users.Commands;

public record UpdateBankAccountCommand(
    Guid UserId,
    string BankId,
    string AccountNumber,
    string AccountName) : ICommand<bool>;
