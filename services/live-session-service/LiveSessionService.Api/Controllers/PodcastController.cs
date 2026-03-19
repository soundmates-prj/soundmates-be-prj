using LiveSessionService.Api.Extensions;
using LiveSessionService.Api.Models.Requests.Podcasts;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Podcasts.Commands.CreatePodcast;
using LiveSessionService.Application.Features.Podcasts.Commands.DeletePodcast;
using LiveSessionService.Application.Features.Podcasts.Commands.UpdatePodcast;
using LiveSessionService.Application.Features.Podcasts.Queries.GetPodcast;
using LiveSessionService.Application.Features.Podcasts.Queries.GetPodcasts;
using LiveSessionService.Application.Features.Results.Podcasts;
using Microsoft.AspNetCore.Mvc;

namespace LiveSessionService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PodcastController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public PodcastController(ICommandDispatcher commands, IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PodcastResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? createdBy,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var query = new GetPodcastsQuery(createdBy, status);
        var result = await _queries.Send<GetPodcastsQuery, List<PodcastResult>>(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<List<PodcastResult>>.SuccessResponse(result.Data!, $"Retrieved {result.Data!.Count} podcast(s)"));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PodcastResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetPodcastQuery(id);
        var result = await _queries.Send<GetPodcastQuery, PodcastResult>(query, ct);

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

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PodcastResult>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Create([FromBody] CreatePodcastRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        var command = new CreatePodcastCommand(
            request.CreatedBy,
            request.Title,
            request.Description,
            request.Author,
            request.Type,
            request.Banner);

        var result = await _commands.Send<CreatePodcastCommand, PodcastResult>(command, ct);

        if (!result.IsSuccess)
        {
            return StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse());
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.ToApiResponse());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PodcastResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePodcastRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        var command = new UpdatePodcastCommand(
            id,
            request.Title,
            request.Description,
            request.Author,
            request.Type,
            request.Banner,
            request.Status);

        var result = await _commands.Send<UpdatePodcastCommand, PodcastResult>(command, ct);

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

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var command = new DeletePodcastCommand(id);
        var result = await _commands.Send(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == ErrorCode.NotFound
                ? NotFound(result.ToApiResponse())
                : StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse());
        }

        return NoContent();
    }
}
