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
/// Handles member-initiated permanent account deletion request.
/// Sets DeletionScheduledAt = now + 30 days. User can still log in to cancel.
/// After grace period, a background job permanently deletes all data.
/// </summary>
public sealed class RequestAccountDeletionHandler : ICommandHandler<RequestAccountDeletionCommand, bool>
{
    private const string RequiredConfirmationText = "XÓA TÀI KHOẢN";

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOutboxRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RequestAccountDeletionHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOutboxRepository outbox,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<bool>> Handle(RequestAccountDeletionCommand command, CancellationToken ct)
    {
        try
        {
            // Step 1: Validate confirmation text
            if (string.IsNullOrWhiteSpace(command.ConfirmationText)
                || !command.ConfirmationText.Trim().Equals(RequiredConfirmationText, StringComparison.Ordinal))
            {
                return Result<bool>.Failure(
                    "Vui lòng nhập chính xác 'XÓA TÀI KHOẢN' để xác nhận",
                    400);
            }

            var user = await _userRepository.GetByIdAsync(command.UserId);
            if (user == null)
                throw new UserNotFoundException($"User with ID {command.UserId} not found");

            // Step 2: Validate password
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(command.Password, user.Password);
            if (!isPasswordValid)
                return Result<bool>.Failure("Mật khẩu không đúng", 401);

            // Step 3: Mark for deletion (sets IsActive = false, DeletionScheduledAt = now + 30 days)
            user.MarkForDeletion(_dateTimeProvider);

            // Step 4: Revoke all refresh tokens
            await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id);

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(ct);

            // Step 5: Publish deletion requested event
            var now = _dateTimeProvider.UtcNow;
            var deletionDate = now.AddDays(30);

            await _outbox.EnqueueAsync(RoutingKeys.Auth.UserDeleted, new UserDeletedEvent
            {
                Id = user.Id,
                DeletedAt = deletionDate,
                Reason = "Member requested permanent deletion"
            }, ct);

            return Result<bool>.Success(true,
                $"Yêu cầu xóa tài khoản đã được ghi nhận. Tài khoản sẽ bị xóa vĩnh viễn vào ngày {deletionDate:dd/MM/yyyy}. Vui lòng đăng nhập trước ngày này để hủy yêu cầu.");
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
