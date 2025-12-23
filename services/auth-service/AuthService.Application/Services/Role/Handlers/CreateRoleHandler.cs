using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Role.Commands;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services.Role.Handlers
{
    public class CreateRoleHandler(IRoleRepository roleRepository, IOutbox outbox, IUnitOfWork unitOfWork) : ICommandHandler<CreateRoleCommand, Guid>
    {
        public async Task<ApiResponse<Guid>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
        {
            // Check if role already exists
            var existingRole = await roleRepository.GetByNameAsync(command.Name);
            if (existingRole != null)
            {
                return ApiResponse<Guid>.FailureResponse($"Role '{command.Name}' already exists", 409);
            }

            var role = new Domain.Entities.UserRole
            {
                Name = command.Name,
            };

            await roleRepository.AddAsync(role);
            
            // Save changes to ensure role ID is generated
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Reload role to ensure ID is set
            role = await roleRepository.GetByIdAsync(role.Id) ?? role;
            
            if (role.Id == Guid.Empty)
            {
                return ApiResponse<Guid>.FailureResponse("Failed to create role: ID was not generated", 500);
            }

            await outbox.EnqueueAsync("auth.role.created", new
            {
                id = role.Id,
                name = role.Name,
            }, cancellationToken);
            
            // Save outbox message
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<Guid>.SuccessResponse(role.Id, $"Create Role {role.Name} Successfully!");
        }
    }
}
