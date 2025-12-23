using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs;

namespace AuthService.Application.Services.Auth.Commands;

public sealed record VerifyEmailCommand : ICommand<UserDto>
{
    public required string Email { get; init; }
    public required string OtpCode { get; init; }
}

