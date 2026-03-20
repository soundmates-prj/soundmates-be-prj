using AuthService.Application.Abstractions;
using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Configuration;
using AuthService.Application.Features.SpotifyAuth.Commands;
using AuthService.Application.Results;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace AuthService.Application.Features.SpotifyAuth.Handlers;

public sealed class ConnectSpotifyHandler : ICommandHandler<ConnectSpotifyCommand, bool>
{
    private readonly ISpotifyTokenRepository _spotifyTokenRepository;
    private readonly ISpotifyUserApiClient _spotifyUserClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SpotifyOptions _opts;

    public ConnectSpotifyHandler(
        ISpotifyTokenRepository spotifyTokenRepository,
        ISpotifyUserApiClient spotifyUserClient,
        IUnitOfWork unitOfWork,
        IOptions<SpotifyOptions> opts)
    {
        _spotifyTokenRepository = spotifyTokenRepository;
        _spotifyUserClient = spotifyUserClient;
        _unitOfWork = unitOfWork;
        _opts = opts.Value;
    }

    public async Task<Result<bool>> Handle(ConnectSpotifyCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
            return Result<bool>.Failure("Code is required", 400);

        if (!TryGetValidatedHttpsRedirectUri(out var redirectUri))
            return Result<bool>.Failure("Spotify RedirectUri must be configured as HTTPS", 400);

        var tokenResponse = await _spotifyUserClient.ExchangeCodeAsync(command.Code.Trim(), redirectUri, cancellationToken);
        if (tokenResponse == null)
            return Result<bool>.Failure("Failed to exchange code", 400);

        var token = await _spotifyTokenRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (token == null)
        {
            token = SpotifyToken.Create(command.UserId, tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.ExpiresIn);
            await _spotifyTokenRepository.AddAsync(token, cancellationToken);
        }
        else
        {
            token.UpdateToken(tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.ExpiresIn);
            await _spotifyTokenRepository.UpdateAsync(token, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, "Spotify connected successfully");
    }

    private bool TryGetValidatedHttpsRedirectUri(out string redirectUri)
    {
        redirectUri = _opts.RedirectUri?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(redirectUri))
            return false;

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttps;
    }
}
