using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;

namespace AuthService.Application.Features.Auth.Commands
{
    /// <summary>
    /// Google login command - returns AuthResult with tokens
    /// </summary>
    public sealed record GoogleLoginCommand : ICommand<AuthResult>
    {
        public string IdToken { get; init; } = null!;
    }
}

