using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Auth.Commands;

/// <summary>
/// Member-initiated permanent account deletion request.
/// Sets DeletionScheduledAt = now + 30 days grace period.
/// User can still log in during grace period to cancel.
/// </summary>
public sealed record RequestAccountDeletionCommand(
    Guid UserId,
    string Password,
    string ConfirmationText
) : ICommand<bool>;
