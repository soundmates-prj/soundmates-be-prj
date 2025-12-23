using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs;

namespace AuthService.Application.Services.Auth.Commands
{
    public sealed record ResetPasswordCommand : ICommand<bool>
    {
        public string Email { get; init; } = null!;
        public string OtpCode { get; init; } = null!;
        public string NewPassword { get; init; } = null!;
    }
}

