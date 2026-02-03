using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Users.Commands;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.Users.Handlers;

public sealed class UpdateUserHandler : ICommandHandler<UpdateUserCommand, bool>
{
    private readonly IUserRepository _repo;
    private readonly IOutboxRepository _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserHandler(
        IUserRepository repo,
        IOutboxRepository outbox,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _repo.GetByIdAsync(command.Id);
        if (user is null)
        {
            return Result<bool>.Failure("User not found", 404);
        }

        // Validate duplicate username (if changed)
        if (user.Username != command.Username)
        {
            var existingByUsername = await _repo.GetByUsernameAsync(command.Username);
            if (existingByUsername != null && existingByUsername.Id != command.Id)
            {
                return Result<bool>.Failure($"Username '{command.Username}' is already taken", 400);
            }
        }

        // Validate duplicate email (if changed)
        if (user.Email != command.Email)
        {
            var existingByEmail = await _repo.GetByEmailAsync(command.Email);
            if (existingByEmail != null && existingByEmail.Id != command.Id)
            {
                return Result<bool>.Failure($"Email '{command.Email}' is already registered", 400);
            }
        }

        // Use domain method instead of directly setting properties
        try
        {
            user.UpdateProfile(
                command.Username,
                command.Email,
                command.FirstName,
                command.LastName,
                command.RoleId,
                _dateTimeProvider);
        }
        catch (UserValidationException ex)
        {
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }

        await _repo.UpdateAsync(user);
        
        // Commit transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload user with role to get role name
        user = await _repo.GetByIdAsync(user.Id);

        await _outbox.EnqueueAsync("auth.user.updated", new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            roleId = user.RoleId,
            roleName = user.Role?.Name,
            isActive = user.IsActive,
            createdAt = user.CreatedAt,
            updatedAt = user.UpdatedAt
        }, cancellationToken);

        return Result<bool>.Success(true, "Update User Successfully!");
    }
}
