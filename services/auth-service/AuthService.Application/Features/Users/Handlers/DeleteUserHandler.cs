using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Users.Commands;
using AuthService.Domain.Interfaces;

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
        await _repo.DeleteAsync(command.Id);
        
        // Commit transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Create outbox event
        await _outbox.EnqueueAsync("auth.user.deleted", new
        {
            command.Id
        }, cancellationToken);

        return Result<bool>.Success(true, $"Delete {command.Id} Successfully!");
    }
}
