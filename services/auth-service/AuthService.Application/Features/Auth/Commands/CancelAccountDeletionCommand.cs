using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Auth.Commands;

/// <summary>
/// Cancels a pending account deletion request within the 30-day grace period.
/// Restores IsActive = true and clears deletion metadata.
/// </summary>
public sealed record CancelAccountDeletionCommand(Guid UserId) : ICommand<bool>;
