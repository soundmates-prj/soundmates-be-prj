using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs;

namespace AuthService.Application.Services.Auth.Commands
{
    public sealed record ForgetPasswordRequestCommand : ICommand<bool>
    {
        public string Email { get; init; } = null!;
    }
}

