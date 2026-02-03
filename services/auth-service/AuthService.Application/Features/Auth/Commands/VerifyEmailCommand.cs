using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;

namespace AuthService.Application.Features.Auth.Commands;

/// <summary>
/// Verify email command - returns AuthResult with tokens after verification
/// </summary>
public sealed record VerifyEmailCommand : ICommand<AuthResult>
{
    public required string Email { get; init; }
    public required string OtpCode { get; init; }
}

