using AuthService.Application.Abstractions;
using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Features.SpotifyAuth.Commands;
using AuthService.Application.Results;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.SpotifyAuth.Handlers;

public sealed class GetSpotifyProfileHandler : ICommandHandler<GetSpotifyProfileCommand, SpotifyUserProfile>
{
    private readonly ISpotifyTokenRepository _spotifyTokenRepository;
    private readonly ISpotifyUserApiClient _spotifyUserClient;
    private readonly IUnitOfWork _unitOfWork;

    public GetSpotifyProfileHandler(
        ISpotifyTokenRepository spotifyTokenRepository,
        ISpotifyUserApiClient spotifyUserClient,
        IUnitOfWork unitOfWork)
    {
        _spotifyTokenRepository = spotifyTokenRepository;
        _spotifyUserClient = spotifyUserClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SpotifyUserProfile>> Handle(GetSpotifyProfileCommand command, CancellationToken cancellationToken)
    {
        var token = await _spotifyTokenRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (token == null)
            return Result<SpotifyUserProfile>.Failure("Spotify not connected", 404);

        if (token.IsExpired() && !string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            var tokenResponse = await _spotifyUserClient.RefreshTokenAsync(token.RefreshToken, cancellationToken);
            if (tokenResponse != null)
            {
                token.UpdateToken(tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.ExpiresIn);
                await _spotifyTokenRepository.UpdateAsync(token, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        var profile = await _spotifyUserClient.GetUserProfileAsync(token.AccessToken, cancellationToken);
        if (profile == null)
            return Result<SpotifyUserProfile>.Failure("Failed to get profile", 400);

        return Result<SpotifyUserProfile>.Success(profile, "Spotify profile retrieved successfully");
    }
}
