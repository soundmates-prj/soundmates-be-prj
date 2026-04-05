using AuthQueryService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Application.Services.Roles.Queries.GetAllRoles;
using AuthQueryService.Application.Services.Roles.Queries.GetRoleById;
using AuthQueryService.Application.Services.Roles.Queries.GetRoleByName;
using AuthQueryService.Application.Services.Roles.Queries.SearchRoles;
using AuthQueryService.Application.Services.Users.Queries.GetUserRole;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthQueryService.Api.Controllers
{
    /// <summary>
    /// Role Management API - Query role information and permissions (Admin only)
    /// </summary>
    [Route("api/v1/roles")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class RoleController : ControllerBase
    {
        private readonly IQueryDispatcher _queries;
        private readonly ILogger<RoleController> _logger;

        public RoleController(
            IQueryDispatcher queries,
            ILogger<RoleController> logger)
        {
            _queries = queries;
            _logger = logger;
        }

        /// <summary>
        /// Get all roles in the system
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of all available roles</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<RoleDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            _logger.LogInformation("Admin retrieving all roles");
            
            var res = await _queries.Query(new GetAllRolesQuery(), ct);
            
            return Ok(res);
        }

        /// <summary>
        /// Get role by ID
        /// </summary>
        /// <param name="id">Role ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Role information</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            _logger.LogInformation("Admin retrieving role {RoleId}", id);
            
            var res = await _queries.Query(new GetRoleByIdQuery(id), ct);
            
            if (!res.Success)
            {
                _logger.LogWarning("Role {RoleId} not found", id);
                return NotFound(res);
            }
            
            return Ok(res);
        }

        /// <summary>
        /// Get role by name
        /// </summary>
        /// <param name="name">Role name (e.g., ADMIN, USER, HOST, STAFF)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Role information</returns>
        [HttpGet("by-name/{name}")]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByName(string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("GetByName called with empty role name");
                return BadRequest(ApiResponse<RoleDto>.FailureResponse("Role name cannot be empty", 400));
            }

            _logger.LogInformation("Admin retrieving role by name: {RoleName}", name);
            
            var res = await _queries.Query(new GetRoleByNameQuery(name), ct);
            
            if (!res.Success)
            {
                _logger.LogWarning("Role with name '{RoleName}' not found", name);
                return NotFound(res);
            }
            
            return Ok(res);
        }

        /// <summary>
        /// Search roles with pagination
        /// </summary>
        /// <param name="q">Search query (role name or description)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Paginated list of roles matching search criteria</returns>
        [AllowAnonymous]
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<RoleDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search(
            [FromQuery] string? q, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20, 
            CancellationToken ct = default)
        {
            _logger.LogInformation("Admin searching roles: Query='{Query}', Page={Page}, PageSize={PageSize}", 
                q ?? "(all)", page, pageSize);
            
            var res = await _queries.Query(new SearchRolesQuery(q, page, pageSize), ct);
            
            return Ok(res);
        }

        /// <summary>
        /// Get role assigned to a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>User's assigned role information</returns>
        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserRole(Guid userId, CancellationToken ct)
        {
            _logger.LogInformation("Admin retrieving role for user {UserId}", userId);
            
            var res = await _queries.Query(new GetUserRoleQuery(userId), ct);
            
            if (!res.Success)
            {
                _logger.LogWarning("Role not found for user {UserId}", userId);
                return NotFound(res);
            }
            
            return Ok(res);
        }
    }
}
