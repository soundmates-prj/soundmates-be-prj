using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Users.Commands;

public sealed record BanUserCommand(Guid UserId, string? Reason = null) : ICommand<bool>;
