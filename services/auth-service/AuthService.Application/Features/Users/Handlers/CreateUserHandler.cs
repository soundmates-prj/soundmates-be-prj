using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Enums;
using AuthService.Application.Results;
using AuthService.Application.Features.Common;
using AuthService.Application.Features.Users.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using Shared.Contracts;
using Shared.Contracts.Events.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Users.Handlers;

/// <summary>
/// Handler for creating new user (Admin action).
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
            // Validate duplicate username
            var existingByUsername = await _repo.GetByUsernameAsync(command.Username);
            if (existingByUsername != null)
                return Result<Guid>.Failure($"Username '{command.Username}' is already taken", 400);

            // Validate duplicate email
            var existingByEmail = await _repo.GetByEmailAsync(command.Email);
            if (existingByEmail != null)
                return Result<Guid>.Failure($"Email '{command.Email}' is already registered", 400);

            // Assign default MEMBER role if not provided
            Guid? roleId = command.RoleId;
            if (!roleId.HasValue)
            {
                var memberRole = await _roleRepository.GetByNameAsync("MEMBER");
                if (memberRole == null)
                    return Result<Guid>.Failure("Default role 'MEMBER' not found. Please ensure roles are seeded.", 500);
                roleId = memberRole.Id;
            }

            var passwordHash = string.IsNullOrWhiteSpace(command.Password)
                ? string.Empty
                : _passwordHasher.HashPassword(command.Password);

            User user;
            try
            {
                user = User.CreateAdminUser(
                    command.Username, command.Email, passwordHash,
                    command.FirstName, command.LastName, roleId, _dateTimeProvider);
            }
            catch (UserValidationException ex)
            {
                return Result<Guid>.Failure(ex.Message, ex.StatusCode);
            }

            await _repo.AddAsync(user);
            user = await _repo.GetByIdAsync(user.Id);

            // Admin-created users are immediately verified (no OTP needed)
            user.VerifyEmail(_dateTimeProvider);
            await _repo.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            user = await _repo.GetByIdAsync(user.Id);

            // Publish typed UserCreatedEvent (Auth — domain state change)
            await _outbox.EnqueueAsync(RoutingKeys.Auth.UserCreated, new UserCreatedEvent
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                RoleId = user.RoleId ?? Guid.Empty,
                RoleName = user.Role?.Name ?? "MEMBER",
                IsActive = user.IsActive,
                AccountStatus = (int)MapToStatus(user),
                IsVerified = true,
                EmailVerifiedAt = user.EmailVerifiedAt ?? _dateTimeProvider.UtcNow,
                CreatedAt = user.CreatedAt ?? _dateTimeProvider.UtcNow
            }, cancellationToken);

            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            return Result<Guid>.Success(user.Id, $"User {fullName} created successfully!");
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure($"An error occurred while creating user: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Mirrors UpdateAccountStatusHandler.MapToStatus — maps domain entity to canonical AccountStatusEnum.
    /// </summary>
    private static AccountStatusEnum MapToStatus(Domain.Entities.User user)
    {
        if (user.IsActive)
            return AccountStatusEnum.Active;
        // Pending deletion takes precedence over deactivated/suspended
        if (user.DeletionScheduledAt.HasValue && user.DeletionScheduledAt > DateTime.UtcNow)
            return AccountStatusEnum.PendingDeletion;
        return AccountStatusEnum.Deactivated;
    }
}
