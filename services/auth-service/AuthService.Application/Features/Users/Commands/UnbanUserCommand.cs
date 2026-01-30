using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Users.Commands;

public sealed record UnbanUserCommand(Guid UserId) : ICommand<bool>;
