using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;

namespace AuthService.Application.Features.Auth.Commands;

/// <summary>
/// Login command - returns AuthResult with tokens
/// </summary>
public sealed class LoginCommand : ICommand<AuthResult>
{
    public required string Identifier { get; init; }
    public required string Password { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
