using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Auth.Commands;
using AuthService.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Services.Auth.Handlers
{
    public sealed class RefreshTokenHandler : ICommandHandler<RefreshTokenCommand, UserDto>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IOutbox _outbox;
        private readonly IUnitOfWork _unitOfWork;

        public RefreshTokenHandler(
            IRefreshTokenRepository refreshTokenRepository,
            IJwtTokenGenerator jwtTokenGenerator,
            IOutbox outbox,
            IUnitOfWork unitOfWork)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
            _outbox = outbox;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<UserDto>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
        {
            // Find refresh token
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(command.RefreshToken);
            
            if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                return ApiResponse<UserDto>.FailureResponse("Invalid or expired refresh token", 401);
            }

            var user = refreshToken.User;
            if (user == null)
            {
                return ApiResponse<UserDto>.FailureResponse("User not found", 404);
            }

            // Revoke old refresh token
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(refreshToken);

            // Generate new token pair
            var (accessToken, newRefreshToken) = _jwtTokenGenerator.GenerateTokenPair(user);

            // Save new refresh token
            var newRefreshTokenEntity = new Domain.Entities.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);

            // Publish event
            await _outbox.EnqueueAsync("auth.user.token.refreshed", new
            {
                user.Id,
                occurredAtUtc = DateTime.UtcNow
            }, cancellationToken);

            // Return user with new tokens
            var dto = new UserDto(user)
            {
                Token = accessToken,
                RefreshToken = newRefreshToken
            };

            return ApiResponse<UserDto>.SuccessResponse(dto, "Token refreshed successfully");
        }
    }
}

