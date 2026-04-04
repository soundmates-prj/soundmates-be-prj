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
/// Handles member-initiated account deactivation.
/// Validates password, sets IsActive = false, stores reason, revokes all tokens.
/// </summary>
public sealed class DeactivateAccountHandler : ICommandHandler<DeactivateAccountCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOutboxRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeactivateAccountHandler(
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

    public async Task<Result<bool>> Handle(DeactivateAccountCommand command, CancellationToken ct)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(command.UserId);
            if (user == null)
                throw new UserNotFoundException($"User with ID {command.UserId} not found");

            // Validate password
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(command.Password, user.Password);
            if (!isPasswordValid)
                return Result<bool>.Failure("Mật khẩu không đúng", 401);

            // Validate reason
            var validReasons = new[] { "Tạm nghỉ", "Quá nhiều thông báo", "Lý do cá nhân", "Khác" };
            if (!validReasons.Contains(command.Reason))
                return Result<bool>.Failure("Lý do không hợp lệ", 400);

            // Deactivate with reason
            user.DeactivateWithReason(command.Reason, _dateTimeProvider);

            // Revoke all refresh tokens
            await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id);

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(ct);

            // Publish event
            var now = _dateTimeProvider.UtcNow;
            await _outbox.EnqueueAsync(RoutingKeys.Auth.UserDeactivated, new UserDeactivatedEvent
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                DeactivatedAt = now
            }, ct);

            return Result<bool>.Success(true,
                "Tài khoản đã được vô hiệu hóa. Bạn có thể đăng nhập lại trong vòng 90 ngày để kích hoạt lại.");
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
