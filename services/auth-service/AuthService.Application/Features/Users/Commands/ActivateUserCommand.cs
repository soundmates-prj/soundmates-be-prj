using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Users.Commands;

/// <summary>
/// Activate a previously deactivated user account (Admin only)
/// State transition: DEACTIVATED → ACTIVE
/// </summary>
public sealed record ActivateUserCommand(Guid UserId) : ICommand<bool>;
