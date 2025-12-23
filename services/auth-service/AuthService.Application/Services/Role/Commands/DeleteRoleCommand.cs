using AuthService.Application.Abstractions.Messaging;
using System;

namespace AuthService.Application.Services.Role.Commands
{
    public sealed class DeleteRoleCommand(Guid id) : ICommand<bool>
    {
        public Guid Id { get; } = id;
    }
}