using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Services.Users.Commands;

public sealed record UnbanUserCommand(Guid UserId) : ICommand<bool>;
