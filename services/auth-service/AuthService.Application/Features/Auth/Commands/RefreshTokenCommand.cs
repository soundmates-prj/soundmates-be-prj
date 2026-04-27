using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;

namespace AuthService.Application.Features.Auth.Commands
{
    /// <summary>
    /// Refresh token command - returns AuthResult with new tokens
    /// </summary>
    public sealed record RefreshTokenCommand : ICommand<AuthResult>
    {
        public string RefreshToken { get; init; } = null!;
    }
}

