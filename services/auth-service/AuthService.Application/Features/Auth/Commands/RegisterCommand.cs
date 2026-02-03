using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;

namespace AuthService.Application.Features.Auth.Commands;

/// <summary>
/// Register command - returns AuthResult with tokens after successful registration
/// </summary>
public sealed class RegisterCommand : ICommand<AuthResult>
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}
