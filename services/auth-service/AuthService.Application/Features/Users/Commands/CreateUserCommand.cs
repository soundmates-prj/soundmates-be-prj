using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Users.Commands;

public sealed class CreateUserCommand : ICommand<Guid>
{
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }

    public string Password { get; init; } = null!;
    public Guid? RoleId { get; init; }
}
