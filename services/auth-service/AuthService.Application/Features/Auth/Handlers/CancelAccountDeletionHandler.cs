using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Exceptions;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using Shared.Contracts;
using Shared.Contracts.Events.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Auth.Handlers;

/// <summary>
/// Cancels a pending account deletion request within the 30-day grace period.
/// Restores IsActive = true and clears deletion metadata.
/// </summary>
public sealed class CancelAccountDeletionHandler : ICommandHandler<CancelAccountDeletionCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IOutboxRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CancelAccountDeletionHandler(
        IUserRepository userRepository,
        IOutboxRepository outbox,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<bool>> Handle(CancelAccountDeletionCommand command, CancellationToken ct)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(command.UserId);
            if (user == null)
                throw new UserNotFoundException($"User with ID {command.UserId} not found");

            // Cancel deletion request (restores IsActive = true)
            user.CancelDeletionRequest(_dateTimeProvider);

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(ct);

            // Publish reactivation event
            await _outbox.EnqueueAsync(RoutingKeys.Auth.UserActivated, new UserActivatedEvent
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email
            }, ct);

            return Result<bool>.Success(true, "Yêu cầu xóa tài khoản đã được hủy. Tài khoản của bạn đã được kích hoạt lại.");
        }
        catch (UserNotFoundException ex)
        {
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }
        catch (InvalidUserStateException ex)
        {
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }
        catch (Exception)
        {
            return Result<bool>.Failure("Có lỗi xảy ra. Vui lòng thử lại sau.", 500);
        }
    }
}
