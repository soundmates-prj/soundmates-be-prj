using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs;

public sealed class RegisterCommand : ICommand<UserDto>
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}