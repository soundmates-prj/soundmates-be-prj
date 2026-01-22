using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Services.Auth.Commands;

public sealed record ResendOtpCommand : ICommand<bool>
{
    public required string Email { get; init; }
}
