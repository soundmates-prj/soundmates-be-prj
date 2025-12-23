using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs;

namespace AuthService.Application.Services.Auth.Commands
{
    public sealed record RefreshTokenCommand : ICommand<UserDto>
    {
        public string RefreshToken { get; init; } = null!;
    }
}

