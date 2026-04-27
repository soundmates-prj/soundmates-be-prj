using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Auth.Commands
{
    public sealed record ResetPasswordCommand : ICommand<bool>
    {
        public string Email { get; init; } = null!;
        public string OtpCode { get; init; } = null!;
        public string NewPassword { get; init; } = null!;
    }
}

