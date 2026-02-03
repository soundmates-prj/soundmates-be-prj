using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Auth.Commands;

public sealed record ForgetPasswordCommand : ICommand<bool>
{
    public required string Email { get; init; }
}

