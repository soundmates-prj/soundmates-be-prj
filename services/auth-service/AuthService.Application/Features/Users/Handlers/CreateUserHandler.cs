using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Common;
using AuthService.Application.Features.Users.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.Users.Handlers;

/// <summary>
/// Handler for creating new user (Admin action)
/// CLEAN ARCHITECTURE: Uses Domain services and methods
/// </summary>
public sealed class CreateUserHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _repo;
    private readonly IRoleRepository _roleRepository;
    private readonly IOutboxRepository _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserHandler(
        IUserRepository repo,
        IRoleRepository roleRepository,
        IOutboxRepository outbox,
        IDateTimeProvider dateTimeProvider,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _roleRepository = roleRepository;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate duplicate username
            var existingByUsername = await _repo.GetByUsernameAsync(command.Username);
            if (existingByUsername != null)
            {
                return Result<Guid>.Failure(
                    $"Username '{command.Username}' is already taken",
                    400);
            }

            // 2. Validate duplicate email
            var existingByEmail = await _repo.GetByEmailAsync(command.Email);
            if (existingByEmail != null)
            {
                return Result<Guid>.Failure(
                    $"Email '{command.Email}' is already registered",
                    400);
            }

            // 3. If RoleId not provided, assign default MEMBER role
            Guid? roleId = command.RoleId;
            if (!roleId.HasValue)
            {
                var memberRole = await _roleRepository.GetByNameAsync("MEMBER");
                if (memberRole == null)
                {
                    return Result<Guid>.Failure(
                        "Default role 'MEMBER' not found. Please ensure roles are seeded.",
                        500);
                }
                roleId = memberRole.Id;
            }

            // 4. Hash password using Domain Service
            var passwordHash = string.IsNullOrWhiteSpace(command.Password)
                ? string.Empty
                : _passwordHasher.HashPassword(command.Password);

            // 5. Use domain factory method to create user
            User user;
            try
            {
                user = User.CreateAdminUser(
                    command.Username,
                    command.Email,
                    passwordHash,
                    command.FirstName,
                    command.LastName,
                    roleId, // Now guaranteed to have a value
                    _dateTimeProvider);
            }
            catch (UserValidationException ex)
            {
                return Result<Guid>.Failure(ex.Message, ex.StatusCode);
            }

            // 6. Persist user
            await _repo.AddAsync(user);

            // 7. Reload user with role to get role name
            user = await _repo.GetByIdAsync(user.Id);

            // 8. Publish domain event
            await _outbox.EnqueueAsync("auth.user.created", new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                roleId = user.RoleId,
                roleName = user.Role?.Name,
                isActive = user.IsActive,
                createdAt = user.CreatedAt
            }, cancellationToken);

            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            return Result<Guid>.Success(
                user.Id,
                $"User {fullName} created successfully!");
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(
                $"An error occurred while creating user: {ex.Message}",
                500);
        }
    }
}
