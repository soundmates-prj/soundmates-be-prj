using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Mappings;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Interfaces;
using Shared.Contracts;
using Shared.Contracts.Events.Activity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Auth.Handlers;

public sealed class RefreshTokenHandler : ICommandHandler<RefreshTokenCommand, AuthResult>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IOutboxRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IOutboxRepository outbox,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AuthResult>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(command.RefreshToken);

        if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt < _dateTimeProvider.UtcNow)
            return Result<AuthResult>.Failure("Invalid or expired refresh token", 401);

        var user = refreshToken.User;
        if (user == null)
            return Result<AuthResult>.Failure("User not found", 404);

        // Revoke old refresh token
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = _dateTimeProvider.UtcNow;
        await _refreshTokenRepository.UpdateAsync(refreshToken);

        // Generate new token pair
        var (accessToken, newRefreshToken) = _jwtTokenGenerator.GenerateTokenPair(user);

        // Save new refresh token
        var newRefreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = _dateTimeProvider.UtcNow.AddDays(7),
            CreatedAt = _dateTimeProvider.UtcNow,
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish typed TokenRefreshedEvent (Activity — audit trail)
        await _outbox.EnqueueAsync(RoutingKeys.Auth.TokenRefreshed, new TokenRefreshedEvent
        {
            UserId = user.Id
        }, cancellationToken);

        var authResult = user.ToAuthResult(accessToken, newRefreshToken);
        return Result<AuthResult>.Success(authResult, "Token refreshed successfully");
    }
}