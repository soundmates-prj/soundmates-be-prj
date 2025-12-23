using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Role.Commands;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services.Role.Handlers
{
    public sealed class DeleteRoleHandler(IRoleRepository roles, IOutbox outbox) : ICommandHandler<DeleteRoleCommand, bool>
    {
        public async Task<ApiResponse<bool>> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
        {
            var role = await roles.GetByIdAsync(command.Id);
            if (role is null)
                return ApiResponse<bool>.FailureResponse("Role not found", 404);

            await roles.DeleteAsync(role);

            await outbox.EnqueueAsync("auth.role.deleted", new
            {
                role.Id,
                role.Name
            }, cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, $"Delete Role {role.Name} Successfully!");
        }
    }
}