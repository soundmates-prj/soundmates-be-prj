using AuthService.Api.Extensions;
using AuthService.Application.Abstractions;
using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.Features.SpotifyAuth.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/v1/spotify-auth")]
public class SpotifyTokensController : ControllerBase
{
    private readonly ICommandDispatcher _commands;

    public SpotifyTokensController(ICommandDispatcher commands)
    {
        _commands = commands;
    }

    [HttpGet("login")]
    [Authorize]
    public async Task<IActionResult> GetLoginUrl(CancellationToken ct)
    {
        var result = await _commands.Send<GetSpotifyLoginUrlCommand, string>(new GetSpotifyLoginUrlCommand(), ct);
        if (!result.IsSuccess)
            return BadRequest(new { Message = result.ErrorMessage ?? "Failed to generate Spotify login URL" });

        return Ok(new { Url = result.Data });
    }

    [HttpPost("connect")]
    [Authorize]
    public async Task<IActionResult> Connect([FromBody] ConnectSpotifyRequest request, CancellationToken ct)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var cmd = new ConnectSpotifyCommand
        {
            UserId = userId,
            Code = request.Code
        };

        var result = await _commands.Send<ConnectSpotifyCommand, bool>(cmd, ct);
        if (!result.IsSuccess)
            return BadRequest(new { Message = result.ErrorMessage ?? "Failed to connect Spotify" });

        return Ok(new { Message = result.ErrorMessage ?? "Spotify connected successfully" });
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var cmd = new GetSpotifyProfileCommand { UserId = userId };
        var result = await _commands.Send<GetSpotifyProfileCommand, SpotifyUserProfile>(cmd, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                404 => NotFound(new { Message = result.ErrorMessage ?? "Spotify not connected" }),
                _ => BadRequest(new { Message = result.ErrorMessage ?? "Failed to get profile" })
            };
        }

        return Ok(result.Data);
    }
}

public class ConnectSpotifyRequest
{
    public string Code { get; set; } = string.Empty;
}
