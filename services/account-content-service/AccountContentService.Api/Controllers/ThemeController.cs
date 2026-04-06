using AccountContentService.Api.Common;
using AccountContentService.Api.Constants;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.Themes.Commands.CreateTheme;
using AccountContentService.Application.Features.Themes.Commands.DeleteTheme;
using AccountContentService.Application.Features.Themes.Commands.SetThemeActive;
using AccountContentService.Application.Features.Themes.Commands.UpdateTheme;
using AccountContentService.Application.Features.Themes.Queries.GetAllThemes;
using AccountContentService.Application.Features.Themes.Queries.GetUserTheme;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountContentService.Api.Controllers
{
    /// <summary>
    /// Provides endpoints for managing themes.
    /// </summary>
    [ApiController]
    [Authorize]
    public class ThemesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public ThemesController(IMediator mediator, IMapper mapper)
        {
            _mapper = mapper;
            _mediator = mediator;
        }


        /// <summary>
        /// Retrieves all themes or filters themes by name.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="themeName">Optional theme name to filter.</param>
        /// <returns>List of themes.</returns>
        [HttpGet(ApiRoutes.Themes.GetAllActive)]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllActive(
            [FromQuery] string? themeName, [FromQuery] PaginationNoFilterRequest request)
        {
            var result = new PaginationResult<ThemeDto>();
            // Nếu có name → filter
            if (!string.IsNullOrEmpty(themeName))
            {
                var query = _mapper.Map<GetAllThemesByNameQuery>(request);

                result = await _mediator.Send(query);
            }

            // Không có → lấy tất cả
            else
            {
                var query = _mapper.Map<GetAllActiveThemesQuery>(request);

                result = await _mediator.Send(query);
            }

            var items = _mapper.Map<IEnumerable<ThemeResponse>>(result.Items);

            var response = new PaginationResponse<ThemeResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<ThemeResponse>>.Ok(response, "Get posts successfully"));
        }

        /// <summary>
        /// Retrieves all themes (Admin only).
        /// </summary>
        /// <param name="request"></param>
        /// <returns>List of themes.</returns>
        [HttpGet(ApiRoutes.Themes.GetAll)]
        public async Task<IActionResult> GetAll(
            [FromQuery] PaginationNoFilterRequest request)
        {

            var query = _mapper.Map<GetAllThemesQuery>(request);

            var result = await _mediator.Send(query);

            var items = _mapper.Map<IEnumerable<ThemeResponse>>(result.Items);

            var response = new PaginationResponse<ThemeResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<ThemeResponse>>.Ok(response, "Get posts successfully"));
        }


        /// <summary>
        /// Retrieves a theme by its unique identifier.
        /// </summary>
        /// <param name="themeId">Theme Id.</param>
        /// <returns>Theme details.</returns>
        [HttpGet(ApiRoutes.Themes.GetById)]
        public async Task<IActionResult> GetById(
            Guid themeId)
        {
            var query = new GetThemeDetailsQuery(themeId);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("Theme not found"));
            }

            var response = _mapper.Map<ThemeResponse>(result);
            return Ok(ApiResponse<ThemeResponse>.Ok(response, "Theme retrieved successfully"));
        }


        // =========================
        // 4. Create
        // =========================

        /// <summary>
        /// Creates a new theme. (Admin only)
        /// </summary>
        /// <param name="request">Theme creation data.</param>
        /// <returns>Created theme Id.</returns>
        [HttpPost(ApiRoutes.Themes.Create)]
        public async Task<IActionResult> Create(
            [FromBody] ThemeRequest request)
        {
            var command = _mapper.Map<CreateThemeCommand>(request);
            var result = await _mediator.Send(command);
            var response = _mapper.Map<ThemeResponse>(result);

            return Ok(ApiResponse<ThemeResponse>.Ok(response, "Create theme successfully"));
        }

        /// <summary>
        /// Updates an existing theme. (Admin only)
        /// </summary>
        /// <param name="themeId">Theme Id.</param>
        /// <param name="request">Updated data.</param>
        [HttpPut(ApiRoutes.Themes.Update)]
        public async Task<IActionResult> Update(
            Guid themeId,
            [FromBody] ThemeRequest request)
        {
            var command = _mapper.Map<UpdateThemeCommand>(request);
            command.Id = themeId;

            var result = await _mediator.Send(command);
            var response = _mapper.Map<ThemeResponse>(result);

            return Ok(ApiResponse<ThemeResponse>.Ok(response, "Update theme successfully"));
        }

        /// <summary>
        /// Deletes a theme by its unique identifier. (Admin only)
        /// </summary>
        /// <param name="themeId">Theme Id.</param>
        [HttpDelete(ApiRoutes.Themes.Delete)]
        public async Task<IActionResult> Delete(Guid themeId)
        {
            var command = new DeleteThemeCommand(themeId);
            var result = await _mediator.Send(command);

            return Ok(ApiResponse<bool>.Ok(result, $"Status: {result}"));
        }

        /// <summary>
        /// Set active of an existing theme. (Admin only)
        /// </summary>
        /// <param name="themeId">Theme Id.</param>
        [HttpPatch(ApiRoutes.Themes.Active)]
        public async Task<IActionResult> SetActiveStatus(
            Guid themeId)
        {
            var command = new SetThemeActiveCommand(themeId);

            var result = await _mediator.Send(command);

            return Ok(ApiResponse<bool>.Ok(result, $"Theme active set to: {result}"));
        }
    }
}