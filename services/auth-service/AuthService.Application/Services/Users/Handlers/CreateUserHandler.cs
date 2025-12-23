using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Users.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services.Users.Handlers;

public sealed class CreateUserHandler(IUserRepository repo, IOutbox outbox) : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<ApiResponse<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Validate duplicate username
        var existingByUsername = await repo.GetByUsernameAsync(command.Username);
        if (existingByUsername != null)
        {
            return ApiResponse<Guid>.FailureResponse($"Username '{command.Username}' is already taken", 400);
        }

        // Validate duplicate email
        var existingByEmail = await repo.GetByEmailAsync(command.Email);
        if (existingByEmail != null)
        {
            return ApiResponse<Guid>.FailureResponse($"Email '{command.Email}' is already registered", 400);
        }

        // Hash password if provided
        var passwordHash = !string.IsNullOrWhiteSpace(command.Password)
            ? BCrypt.Net.BCrypt.HashPassword(command.Password)
            : null;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = command.Username,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            RoleId = command.RoleId,
            Password = passwordHash ?? string.Empty,
            IsActive = true, // Admin-created users are active by default
            CreatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(user);

        // Reload user with role to get role name
        user = await repo.GetByIdAsync(user.Id);

        await outbox.EnqueueAsync("auth.user.created", new
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
        return ApiResponse<Guid>.SuccessResponse(user.Id, $"Create User {fullName} Successfully!");
    }
}