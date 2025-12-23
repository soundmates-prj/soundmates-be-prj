using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs;

public sealed class LoginCommand : ICommand<UserDto>
{
    public required string EmailOrUsername { get; init; }
    public required string Password { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}