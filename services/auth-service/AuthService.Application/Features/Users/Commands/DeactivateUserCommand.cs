using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Users.Commands;

public sealed record DeactivateUserCommand(Guid UserId) : ICommand<bool>;
