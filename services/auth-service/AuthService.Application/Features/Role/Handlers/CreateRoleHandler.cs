using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Role.Commands;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.Role.Handlers
{
    public class CreateRoleHandler : ICommandHandler<CreateRoleCommand, Guid>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IOutboxRepository _outbox;
        private readonly IUnitOfWork _unitOfWork;

        public CreateRoleHandler(
            IRoleRepository roleRepository,
            IOutboxRepository outbox,
            IUnitOfWork unitOfWork)
        {
            _roleRepository = roleRepository;
            _outbox = outbox;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
        {
            // Check if role already exists
            var existingRole = await _roleRepository.GetByNameAsync(command.Name);
            if (existingRole != null)
            {
                return Result<Guid>.Failure($"Role '{command.Name}' already exists", 409);
            }

            var role = new Domain.Entities.UserRole
            {
                Name = command.Name,
            };

            await _roleRepository.AddAsync(role);
            
            // Save changes to ensure role ID is generated
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Reload role to ensure ID is set
            role = await _roleRepository.GetByIdAsync(role.Id) ?? role;
            
            if (role.Id == Guid.Empty)
            {
                return Result<Guid>.Failure("Failed to create role: ID was not generated", 500);
            }

            await _outbox.EnqueueAsync("auth.role.created", new
            {
                id = role.Id,
                name = role.Name,
            }, cancellationToken);
            
            // Save outbox message
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(role.Id, $"Create Role {role.Name} Successfully!");
        }
    }
}
