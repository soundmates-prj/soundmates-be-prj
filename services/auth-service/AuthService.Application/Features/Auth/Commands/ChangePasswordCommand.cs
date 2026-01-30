using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Auth.Commands
{
    public sealed record ChangePasswordCommand : ICommand<bool>
    {
        public Guid UserId { get; init; }
        public string OldPassword { get; init; } = null!;
        public string NewPassword { get; init; } = null!;
    }
}

