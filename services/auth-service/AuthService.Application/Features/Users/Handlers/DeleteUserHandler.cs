using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Users.Commands;
using AuthService.Domain.Interfaces;
using Shared.Contracts;
using Shared.Contracts.Events.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Users.Handlers;

public sealed class DeleteUserHandler : ICommandHandler<DeleteUserCommand, bool>
{
    private readonly IUserRepository _repo;
    private readonly IOutboxRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserHandler(
        IUserRepository repo,
        IOutboxRepository outbox,
        IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _repo.GetByIdAsync(command.Id);
        await _repo.DeleteAsync(command.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish typed UserDeletedEvent (Auth — domain state change)
        await _outbox.EnqueueAsync(RoutingKeys.Auth.UserDeleted, new UserDeletedEvent
        {
            Id = user?.Id ?? command.Id,
            DeletedAt = DateTime.UtcNow,
            Reason = "Hard delete by admin"
        }, cancellationToken);

        return Result<bool>.Success(true, $"Delete {command.Id} Successfully!");
    }
}
