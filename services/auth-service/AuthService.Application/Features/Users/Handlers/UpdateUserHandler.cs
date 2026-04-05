using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Users.Commands;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using Shared.Contracts;
using Shared.Contracts.Events.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;

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
            return Result<bool>.Failure("User not found", 404);

        if (user.Username != command.Username)
        {
            var existing = await _repo.GetByUsernameAsync(command.Username);
            if (existing != null && existing.Id != command.Id)
                return Result<bool>.Failure($"Username '{command.Username}' is already taken", 400);
        }

        if (user.Email != command.Email)
        {
            var existing = await _repo.GetByEmailAsync(command.Email);
            if (existing != null && existing.Id != command.Id)
                return Result<bool>.Failure($"Email '{command.Email}' is already registered", 400);
        }

        try
        {
            user.UpdateProfile(
                command.Username, command.Email,
                command.FirstName, command.LastName,
                command.RoleId, _dateTimeProvider);
        }
        catch (UserValidationException ex)
        {
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }

        await _repo.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        user = await _repo.GetByIdAsync(user.Id);

        // Publish typed UserUpdatedEvent (Auth — domain state change)
        await _outbox.EnqueueAsync(RoutingKeys.Auth.UserUpdated, new UserUpdatedEvent
        {
            Id = user!.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            RoleId = user.RoleId ?? Guid.Empty,
            RoleName = user.Role?.Name,
            IsActive = user.IsActive,
            UpdatedAt = user.UpdatedAt ?? DateTime.UtcNow
        }, cancellationToken);

        return Result<bool>.Success(true, "Update User Successfully!");
    }
}
