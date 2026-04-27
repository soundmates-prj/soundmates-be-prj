using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Users.Commands;

public sealed class DeleteUserCommand(Guid id) : ICommand<bool>
{
    public Guid Id { get; } = id;
}
