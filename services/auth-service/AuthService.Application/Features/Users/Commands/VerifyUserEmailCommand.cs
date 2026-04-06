using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Users.Commands;

/// <summary>
/// Manually verify a user's email (Admin only)
/// Activates the user account without requiring OTP verification.
/// Used when a user has not received or cannot use OTP.
/// </summary>
public sealed record VerifyUserEmailCommand(Guid UserId) : ICommand<bool>;
