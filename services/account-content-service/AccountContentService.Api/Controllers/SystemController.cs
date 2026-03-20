
using AccountContentService.Api.Common;
using AccountContentService.Api.Constants;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.Features.SystemSettings.Commands.CreateSetting;
using AccountContentService.Application.Features.SystemSettings.Commands.DeleteSetting;
using AccountContentService.Application.Features.SystemSettings.Commands.UpdateSetting;
using AccountContentService.Application.Features.SystemSettings.Queries.GetSettings;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountContentService.Api.Controllers
{
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

    }
}
