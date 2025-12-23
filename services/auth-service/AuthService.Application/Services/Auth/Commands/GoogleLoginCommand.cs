using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs;

namespace AuthService.Application.Services.Auth.Commands
{
    public sealed record GoogleLoginCommand : ICommand<UserDto>
    {
        public string IdToken { get; init; } = null!;
    }
}

