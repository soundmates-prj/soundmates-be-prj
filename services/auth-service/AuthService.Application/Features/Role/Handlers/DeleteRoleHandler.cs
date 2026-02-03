using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Role.Commands;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.Role.Handlers
{
    public sealed class DeleteRoleHandler : ICommandHandler<DeleteRoleCommand, bool>
    {
        private readonly IRoleRepository _roles;
        private readonly IOutboxRepository _outbox;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteRoleHandler(
            IRoleRepository roles,
            IOutboxRepository outbox,
            IUnitOfWork unitOfWork)
        {
            _roles = roles;
            _outbox = outbox;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
        {
            var role = await _roles.GetByIdAsync(command.Id);
            if (role is null)
                return Result<bool>.Failure("Role not found", 404);

            await _roles.DeleteAsync(role);
            
            // Commit transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _outbox.EnqueueAsync("auth.role.deleted", new
            {
                role.Id,
                role.Name
            }, cancellationToken);

            return Result<bool>.Success(true, $"Delete Role {role.Name} Successfully!");
        }
    }
}
