using LiveSessionService.Api.Models.Requests.LiveSessions;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Application.Features.PodcastEpisodeRequests.Commands.CreatePodcastEpisodeRequest;
using LiveSessionService.Application.Features.PodcastEpisodeRequests.Commands.ReviewPodcastEpisodeRequest;
using LiveSessionService.Application.Features.PodcastEpisodeRequests.Queries.GetPodcastEpisodeRequests;
using LiveSessionService.Application.Features.PodcastEpisodeRequests.Queries.GetMyPodcastEpisodeRequests;
using LiveSessionService.Application.Features.PodcastEpisodeRequests.Queries.GetPodcastEpisodeRequestById;
using LiveSessionService.Application.Features.PodcastEpisodeRequests.Commands.CheckPodcastEpisodeToxicity;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveSessionService.Api.Controllers;

[ApiController]
[Route("api/v1/podcast-episode-requests")]
public class PodcastEpisodeRequestController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public PodcastEpisodeRequestController(ICommandDispatcher commands, IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var nameIdentifier = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(nameIdentifier, out userId);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PodcastEpisodeRequestResult>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePodcastEpisodeRequestRequest request,
        CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var authorName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value 
            ?? User.FindFirst("unique_name")?.Value
            ?? User.FindFirst("name")?.Value 
            ?? User.FindFirst("preferred_username")?.Value 
            ?? "SoundMates Member";
        var authorAvatar = User.FindFirst("picture")?.Value;
        var authorEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;

        var authorInfoStr = System.Text.Json.JsonSerializer.Serialize(new {
            Name = authorName,
            Avatar = authorAvatar,
            Email = authorEmail,
            UserId = userId
        });

        var command = new CreatePodcastEpisodeRequestCommand(
            PodcastId: request.PodcastId,
            RequestedByUserId: userId,
            AuthorInfo: authorInfoStr,
            Title: request.Title,
            Description: request.Description,
            ThumbnailUrl: request.ThumbnailUrl,
            AudioUrl: request.AudioUrl,
            Duration: request.Duration);

        var result = await _commands.Send<CreatePodcastEpisodeRequestCommand, PodcastEpisodeRequestResult>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Data!.Id },
            result.ToApiResponse());
    }

    [HttpGet]
    [Authorize(Roles = "STAFF,ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<List<PodcastEpisodeRequestResult>>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var query = new GetPodcastEpisodeRequestsQuery(status, search);
        var result = await _queries.Send<GetPodcastEpisodeRequestsQuery, List<PodcastEpisodeRequestResult>>(query, ct);

        if (!result.IsSuccess)
            return StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse());

        return Ok(ApiResponse<List<PodcastEpisodeRequestResult>>.SuccessResponse(
            result.Data!,
            $"Retrieved {result.Data!.Count} episode requests"));
    }

    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<PodcastEpisodeRequestResult>>), 200)]
    public async Task<IActionResult> GetMy(
        [FromQuery] string? status,
        CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var query = new GetMyPodcastEpisodeRequestsQuery(userId, status);
        var result = await _queries.Send<GetMyPodcastEpisodeRequestsQuery, List<PodcastEpisodeRequestResult>>(query, ct);

        if (!result.IsSuccess)
            return StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse());

        return Ok(ApiResponse<List<PodcastEpisodeRequestResult>>.SuccessResponse(
            result.Data!,
            $"Retrieved {result.Data!.Count} your episode requests"));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PodcastEpisodeRequestResult>), 200)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetPodcastEpisodeRequestByIdQuery(id);
        var result = await _queries.Send<GetPodcastEpisodeRequestByIdQuery, PodcastEpisodeRequestResult>(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
    }

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = "STAFF,ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<PodcastEpisodeRequestResult>), 200)]
    public async Task<IActionResult> Review(
        Guid id,
        [FromBody] ReviewPodcastEpisodeRequestRequest request,
        CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var reviewerId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var command = new ReviewPodcastEpisodeRequestCommand(
            RequestId: id,
            IsApproved: request.IsApproved,
            RejectReason: request.RejectReason,
            ReviewedByUserId: reviewerId);

        var result = await _commands.Send<ReviewPodcastEpisodeRequestCommand, PodcastEpisodeRequestResult>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
    }

    [HttpPost("{id:guid}/check-toxicity")]
    [Authorize(Roles = "STAFF,ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<ModerationResult>), 200)]
    public async Task<IActionResult> CheckToxicity(Guid id, CancellationToken ct)
    {
        var command = new CheckPodcastEpisodeToxicityCommand(id);
        var result = await _commands.Send<CheckPodcastEpisodeToxicityCommand, ModerationResult>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<ModerationResult>.SuccessResponse(result.Data!, "Toxicity check completed successfully"));
    }
}
