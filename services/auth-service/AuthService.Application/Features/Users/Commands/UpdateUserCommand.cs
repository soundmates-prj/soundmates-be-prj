using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Users.Commands;

public sealed class UpdateUserCommand(Guid id) : ICommand<bool>
{
    public Guid Id { get; } = id;
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public Guid? RoleId { get; init; }
}
