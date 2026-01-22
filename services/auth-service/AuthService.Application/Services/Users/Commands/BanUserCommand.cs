using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Services.Users.Commands;

public sealed record BanUserCommand(Guid UserId, string? Reason = null) : ICommand<bool>;
