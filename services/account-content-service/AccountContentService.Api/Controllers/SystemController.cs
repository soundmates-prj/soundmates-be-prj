
using AccountContentService.Api.Common;
using AccountContentService.Api.Constants;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.Features.ServiceConfigs.Commands.DeleteAzuraCastConfig;
using AccountContentService.Application.Features.ServiceConfigs.Commands.UpsertAzuraCastConfig;
using AccountContentService.Application.Features.ServiceConfigs.Queries.GetAzuraCastConfig;
using AccountContentService.Application.Features.SystemSettings.Commands.CreateSetting;
using AccountContentService.Application.Features.SystemSettings.Commands.DeleteSetting;
using AccountContentService.Application.Features.SystemSettings.Commands.UpdateSetting;
using AccountContentService.Application.Features.SystemSettings.Commands.UpsertGeminiKey;
using AccountContentService.Application.Features.SystemSettings.Commands.DeleteGeminiConfig;
using AccountContentService.Application.Features.SystemSettings.Queries.GetGeminiConfig;
using AccountContentService.Application.Features.SystemSettings.Queries.GetSettings;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountContentService.Api.Controllers
{
    /// <summary>
    /// Provides endpoints for system settings and external service configurations.
    /// </summary>
    /// <summary>
    /// Provides endpoints for system settings and external service configurations.
    /// </summary>
    [Authorize]
    [ApiController]
    public class SystemController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public SystemController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        /// <summary>
        /// Create a new system setting.
        /// </summary>
        /// <remarks>
        /// Allows administrators to create a new system configuration setting.
        /// The setting may include sensitive values that will be securely stored.
        /// </remarks>
        /// <response code="200">System setting created successfully</response>
        /// <response code="400">Invalid request data</response>
        [HttpPost(ApiRoutes.Settings.Create)]
        public async Task<IActionResult> CreateSetting(SystemSettingRequest request)
        {
            var command = _mapper.Map<CreateSettingCommand>(request);

            var result = await _mediator.Send(command);
            var response = _mapper.Map<SystemSettingResponse>(result);

            return Ok(ApiResponse<SystemSettingResponse>.Ok(response, "Create post successfully"));
        }

        /// <summary>
        /// Update an existing system setting.
        /// </summary>
        /// <param name="settingId">The unique identifier of the system setting</param>
        /// <param name="request">Updated setting data</param>
        /// <response code="200">System setting updated successfully</response>
        /// <response code="404">System setting not found</response>
        [HttpPut(ApiRoutes.Settings.Update)]
        public async Task<IActionResult> UpdateSetting(
            [FromRoute] Guid settingId,
            SystemSettingRequest request)
        {
            var command = _mapper.Map<UpdateSettingCommand>(request);
            command.Id = settingId;

            var result = await _mediator.Send(command);
            var response = _mapper.Map<SystemSettingResponse>(result);

            return Ok(ApiResponse<SystemSettingResponse>.Ok(response, "Update successfully"));
        }

        /// <summary>
        /// Delete a system setting.
        /// </summary>
        /// <param name="settingId">The unique identifier of the system setting</param>
        /// <response code="200">System setting deleted successfully</response>
        /// <response code="404">System setting not found</response>
        [HttpDelete(ApiRoutes.Settings.Delete)]
        public async Task<IActionResult> DeletePostById([FromRoute] Guid settingId)
        {
            var command = new DeleteSettingCommand(settingId);
            var result = await _mediator.Send(command);

            return Ok(ApiResponse<bool>.Ok(result, $"Status: {result}"));
        }

        /// <summary>
        /// Get system setting by ID.
        /// </summary>
        /// <param name="settingId">The unique identifier of the system setting</param>
        /// <response code="200">System setting retrieved successfully</response>
        /// <response code="404">System setting not found</response>
        [HttpGet(ApiRoutes.Settings.GetById)]
        public async Task<IActionResult> GetSettingById([FromRoute] Guid settingId)
        {
            var query = new GetSettingDetailByIdQuery(settingId);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("Setting not found"));
            }
            var response = _mapper.Map<SystemSettingResponse>(result);

            return Ok(ApiResponse<SystemSettingResponse>.Ok(response, "Get setting successfully"));
        }

        /// <summary>
        /// Get system setting by key.
        /// </summary>
        /// <param name="key">The unique key of the system setting</param>
        /// <response code="200">System setting retrieved successfully</response>
        /// <response code="404">System setting not found</response>
        [HttpGet(ApiRoutes.Settings.GetByKey)]
        public async Task<IActionResult> GetSettingByKey([FromRoute] string key)
        {
            var query = new GetSettingDetailByKeyQuery(key);

            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("Setting not found"));
            }
            var response = _mapper.Map<SystemSettingResponse>(result);

            return Ok(ApiResponse<SystemSettingResponse>.Ok(response, "Get setting successfully"));
        }

        /// <summary>
        /// Patch a system setting value by key. Creates it if it doesn't exist.
        /// </summary>
        /// <param name="key">The unique key of the system setting</param>
        /// <param name="request">The value to update</param>
        /// <response code="200">System setting patched successfully</response>
        [HttpPatch(ApiRoutes.Settings.PatchByKey)]
        public async Task<IActionResult> PatchSettingByKey([FromRoute] string key, [FromBody] PatchSettingByKeyRequest request)
        {
            var command = new AccountContentService.Application.Features.SystemSettings.Commands.PatchSettingByKey.PatchSettingByKeyCommand
            {
                Key = key,
                Value = request.Value
            };

            var result = await _mediator.Send(command);
            var response = _mapper.Map<SystemSettingResponse>(result);

            return Ok(ApiResponse<SystemSettingResponse>.Ok(response, "Setting patched successfully"));
        }

        /// <summary>
        /// Get all system settings with pagination.
        /// </summary>
        /// <param name="request">Pagination parameters</param>
        /// <response code="200">System settings retrieved successfully</response>
        /// <response code="404">No system settings found</response>
        [HttpGet(ApiRoutes.Settings.GetAll)]
        public async Task<IActionResult> GetAllSettings([FromQuery] PaginationNoFilterRequest request)
        {
            var query = _mapper.Map<GetSettingsQuery>(request);

            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("Settings not found"));
            }
            var items = _mapper.Map<IEnumerable<SystemSettingResponse>>(result.Items);

            var response = new PaginationResponse<SystemSettingResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<SystemSettingResponse>>.Ok(response, "Get settings successfully"));
        }

        /// <summary>
        /// Upsert Gemini API key configuration.
        /// Encrypts the API key and stores it in SystemSettings.
        /// Publishes GeminiConfigUpdatedEvent to RabbitMQ so ai-service can update.
        /// </summary>
        /// <response code="200">Gemini config upserted successfully</response>
        /// <response code="400">Invalid request data</response>
        [HttpPost(ApiRoutes.Settings.UpsertGeminiKey)]
        public async Task<IActionResult> UpsertGeminiKey(
            [FromBody] GeminiConfigRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpsertGeminiKeyCommand(
                request.Provider,
                request.ApiKey,
                request.IsActive);

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(ApiResponse<UpsertGeminiKeyResult>.Ok(result, "Gemini config updated"));
        }

        /// <summary>
        /// Get Gemini API key configuration.
        /// Returns masked API key (only last 4 chars visible).
        /// </summary>
        /// <response code="200">Gemini config retrieved</response>
        [HttpGet(ApiRoutes.Settings.GetGeminiConfig)]
        public async Task<IActionResult> GetGeminiConfig(CancellationToken cancellationToken)
        {
            var query = new GetGeminiConfigQuery();
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(ApiResponse<GetGeminiConfigResult?>.Ok(result, "Gemini config retrieved"));
        }

        /// <summary>
        /// Delete Gemini API key configuration.
        /// Publishes GeminiConfigUpdatedEvent with IsDeleted=true.
        /// </summary>
        /// <response code="200">Gemini config deleted</response>
        [HttpDelete(ApiRoutes.Settings.DeleteGeminiConfig)]
        public async Task<IActionResult> DeleteGeminiConfig(CancellationToken cancellationToken)
        {
            var command = new DeleteGeminiConfigCommand();
            var deleted = await _mediator.Send(command, cancellationToken);

            if (!deleted)
            {
                return NotFound(ApiResponse<string>.Fail("Gemini config not found"));
            }

            return Ok(ApiResponse<bool>.Ok(true, "Gemini config deleted"));
        }

        /// <summary>
        /// Upsert AzuraCast API key configuration.
        /// Validates the API key against AzuraCast, encrypts and stores it.
        /// Publishes AzuraCastConfigUpdatedEvent to RabbitMQ so live-session-service can update.
        /// </summary>
        /// <response code="200">AzuraCast config upserted successfully</response>
        /// <response code="400">Invalid request or API key validation failed</response>
        [Authorize(Roles = "ADMIN")]
        [HttpPost(ApiRoutes.Settings.UpsertAzuraCastConfig)]
        public async Task<IActionResult> UpsertAzuraCastConfig(
            [FromBody] AzuraCastConfigRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpsertAzuraCastConfigCommand(
                request.BaseUrl,
                request.ApiKey,
                request.IsActive);

            var result = await _mediator.Send(command, cancellationToken);
            var response = new AzuraCastConfigResponse
            {
                BaseUrl = result.BaseUrl,
                MaskedApiKey = string.Empty, // Never return API key
                IsConfigured = result.IsConfigured,
                IsActive = result.IsActive,
                UpdatedAt = result.UpdatedAt
            };

            return Ok(ApiResponse<AzuraCastConfigResponse>.Ok(response, "AzuraCast config updated"));
        }

        /// <summary>
        /// Get AzuraCast API key configuration.
        /// Returns masked API key (only last 4 chars visible).
        /// </summary>
        /// <response code="200">AzuraCast config retrieved</response>
        [Authorize(Roles = "ADMIN")]
        [HttpGet(ApiRoutes.Settings.GetAzuraCastConfig)]
        public async Task<IActionResult> GetAzuraCastConfig(CancellationToken cancellationToken)
        {
            var query = new GetAzuraCastConfigQuery();
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(ApiResponse<GetAzuraCastConfigResult>.Ok(result, "AzuraCast config retrieved"));
        }

        /// <summary>
        /// Delete AzuraCast API key configuration.
        /// Publishes AzuraCastConfigUpdatedEvent with IsDeleted=true.
        /// </summary>
        /// <response code="200">AzuraCast config deleted</response>
        [Authorize(Roles = "ADMIN")]
        [HttpDelete(ApiRoutes.Settings.DeleteAzuraCastConfig)]
        public async Task<IActionResult> DeleteAzuraCastConfig(CancellationToken cancellationToken)
        {
            var command = new DeleteAzuraCastConfigCommand();
            var deleted = await _mediator.Send(command, cancellationToken);

            if (!deleted)
            {
                return NotFound(ApiResponse<string>.Fail("AzuraCast config not found"));
            }

            return Ok(ApiResponse<bool>.Ok(true, "AzuraCast config deleted"));
        }

    }
}
