using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs;

namespace AuthService.Application.Services.Auth.Commands
{
    public sealed record ChangePasswordCommand : ICommand<bool>
    {
        public Guid UserId { get; init; }
        public string OldPassword { get; init; } = null!;
        public string NewPassword { get; init; } = null!;
    }
}

