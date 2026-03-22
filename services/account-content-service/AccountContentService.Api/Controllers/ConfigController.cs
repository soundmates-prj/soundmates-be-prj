using AccountContentService.Api.Common;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Application.Features.ServiceConfigs.Commands.UpsertGeminiConfig;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountContentService.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/config")]
public sealed class ConfigController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConfigController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("gemini")]
    public async Task<IActionResult> UpsertGemini([FromBody] GeminiConfigRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpsertGeminiConfigCommand(
                request.Provider,
                request.ApiKey,
                request.IsActive),
            cancellationToken);

        return Ok(ApiResponse<UpsertGeminiConfigResult>.Ok(result, "Gemini config updated"));
    }
}
