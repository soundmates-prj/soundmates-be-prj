using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Configuration;
using AuthService.Application.Features.SpotifyAuth.Commands;
using AuthService.Application.Results;
using Microsoft.Extensions.Options;

namespace AuthService.Application.Features.SpotifyAuth.Handlers;

public sealed class GetSpotifyLoginUrlHandler : ICommandHandler<GetSpotifyLoginUrlCommand, string>
{
    private readonly SpotifyOptions _opts;

    public GetSpotifyLoginUrlHandler(IOptions<SpotifyOptions> opts)
    {
        _opts = opts.Value;
    }

    public Task<Result<string>> Handle(GetSpotifyLoginUrlCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_opts.ClientId))
            return Task.FromResult(Result<string>.Failure("Spotify client id is not configured", 400));

        if (!TryGetValidatedHttpsRedirectUri(out var redirectUri))
            return Task.FromResult(Result<string>.Failure("Spotify RedirectUri must be configured as HTTPS", 400));

        var scope = "user-read-private user-read-email";
        var url = $"https://accounts.spotify.com/authorize?response_type=code&client_id={_opts.ClientId}&scope={Uri.EscapeDataString(scope)}&redirect_uri={Uri.EscapeDataString(redirectUri)}";

        return Task.FromResult(Result<string>.Success(url, "Spotify login URL generated successfully"));
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
