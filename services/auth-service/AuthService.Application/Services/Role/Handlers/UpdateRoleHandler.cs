using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Role.Commands;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services.Role.Handlers
{
    public sealed class UpdateRoleHandler(IRoleRepository roles, IOutbox outbox) : ICommandHandler<UpdateRoleCommand, bool>
    {
        public async Task<ApiResponse<bool>> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
        {
            var role = await roles.GetByIdAsync(command.Id);
            if (role is null)
                return ApiResponse<bool>.FailureResponse("Role not found", 404);

            role.Name = command.Name;

            await roles.UpdateAsync(role);

            await outbox.EnqueueAsync("auth.role.updated", new
            {
                role.Id,
                role.Name
            }, cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, $"Update Role {role.Name} Successfully!");
        }
    }
}