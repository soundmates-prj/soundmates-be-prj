using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Enums;

namespace AuthService.Application.Features.Users.Commands;

/// <summary>
/// Update user account status (PATCH semantics — idempotent).
/// Replaces: Deactivate, Activate, Ban, Unban
/// </summary>
public sealed record UpdateAccountStatusCommand(
    Guid UserId,
    AccountStatusEnum Status,
    string? Reason = null,
    string? Note = null
) : ICommand<bool>;
