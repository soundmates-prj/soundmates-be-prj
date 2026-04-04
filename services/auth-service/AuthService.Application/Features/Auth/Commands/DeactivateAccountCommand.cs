using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Auth.Commands;

/// <summary>
/// Member-initiated account deactivation.
/// Validates password, then sets IsActive = false and stores the reason.
/// </summary>
public sealed record DeactivateAccountCommand(
    Guid UserId,
    string Password,
    string Reason,
    string? AdditionalNote = null
) : ICommand<bool>;
