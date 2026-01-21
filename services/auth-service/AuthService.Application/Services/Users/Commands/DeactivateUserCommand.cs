using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Services.Users.Commands;

public sealed record DeactivateUserCommand(Guid UserId) : ICommand<bool>;
