using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Application.Services.Users.Queries.GetUserById;
using AuthQueryService.Application.Services.Users.Queries.GetUserByUsername;
using AuthQueryService.Application.Services.Users.Queries.GetUsersByRole;
using AuthQueryService.Application.Services.Users.Queries.SearchUsers;
using AuthQueryService.Application.Services.Users.Queries.GetFullUserProfile;
using AuthQueryService.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace AuthQueryService.Api.Controllers
{
    /// <summary>
    /// User Management API - Query user profiles and information
    /// </summary>
    [Route("api/v1/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IMessageBusPublisher _messageBusPublisher;
        private readonly IQueryDispatcher _queries;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IMessageBusPublisher messageBusPublisher, 
            IQueryDispatcher queries,
            ILogger<UserController> logger)
        {
            _messageBusPublisher = messageBusPublisher;
            _queries = queries;
            _logger = logger;
        }

        /// <summary>
        /// Get current authenticated user's basic profile
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Current user's basic profile information</returns>
        [Authorize]
        [HttpGet("me/profile")]
        [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) 
                           ?? User.FindFirst("sub") 
                           ?? User.Claims.FirstOrDefault(c => c.Type == "user_id");
            
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Invalid or missing user token in GetMyProfile");
                return Unauthorized(ApiResponse<UserReadDto>.FailureResponse("Invalid or missing user token", 401));
            }

            _logger.LogInformation("Retrieving profile for authenticated user {UserId}", userId);
            
            var res = await _queries.Query(new GetUserByIdQuery(userId), ct);
            
            if (!res.Success)
            {
                _logger.LogWarning("Profile not found for user {UserId}", userId);
                return NotFound(res);
            }
            
            return Ok(res);
        }

        /// <summary>
        /// Get current authenticated user's full profile (including roles and additional data)
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Current user's complete profile information</returns>
        [Authorize]
        [HttpGet("me/profile/full")]
        [ProducesResponseType(typeof(ApiResponse<UserFullProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserFullProfileDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<UserFullProfileDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyFullProfile(CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) 
                           ?? User.FindFirst("sub") 
                           ?? User.Claims.FirstOrDefault(c => c.Type == "user_id");
            
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Invalid or missing user token in GetMyFullProfile");
                return Unauthorized(ApiResponse<UserFullProfileDto>.FailureResponse("Invalid or missing user token", 401));
            }

            _logger.LogInformation("Retrieving full profile for authenticated user {UserId}", userId);
            
            var res = await _queries.Query(new GetFullUserProfileQuery(userId), ct);
            
            if (!res.Success)
            {
                _logger.LogWarning("Full profile not found for user {UserId}", userId);
                return NotFound(res);
            }
            
            return Ok(res);
        }

        /// <summary>
        /// Get user by ID (Admin only)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>User basic information</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            _logger.LogInformation("Admin retrieving user {UserId}", id);
            
            var res = await _queries.Query(new GetUserByIdQuery(id), ct);
            
            if (!res.Success)
            {
                _logger.LogWarning("User {UserId} not found", id);
                return NotFound(res);
            }
            
            return Ok(res);
        }

        /// <summary>
        /// Get user's basic profile by ID (Admin only)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>User's basic profile information</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id:guid}/profile")]
        [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfileById(Guid id, CancellationToken ct)
        {
            _logger.LogInformation("Admin retrieving basic profile for user {UserId}", id);
            
            var res = await _queries.Query(new GetUserByIdQuery(id), ct);
            
            if (!res.Success)
            {
                _logger.LogWarning("Profile not found for user {UserId}", id);
                return NotFound(res);
            }
            
            return Ok(res);
        }

        /// <summary>
        /// Get user's full profile by ID (Admin only)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>User's complete profile including roles and additional data</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id:guid}/profile/full")]
        [ProducesResponseType(typeof(ApiResponse<UserFullProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserFullProfileDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFullProfileById(Guid id, CancellationToken ct)
        {
            _logger.LogInformation("Admin retrieving full profile for user {UserId}", id);
            
            var res = await _queries.Query(new GetFullUserProfileQuery(id), ct);
            
            if (!res.Success)
            {
                _logger.LogWarning("Full profile not found for user {UserId}", id);
                return NotFound(res);
            }
            
            return Ok(res);
        }

        /// <summary>
        /// Get user's public profile by ID (Accessible to anyone)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>User's public profile information</returns>
        [AllowAnonymous]
        [HttpGet("{id:guid}/public-profile")]
        [ProducesResponseType(typeof(ApiResponse<UserFullProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserFullProfileDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPublicProfile(Guid id, CancellationToken ct)
        {
            _logger.LogInformation("Retrieving public profile for user {UserId}", id);
            
            var res = await _queries.Query(new GetFullUserProfileQuery(id), ct);
            
            if (!res.Success)
            {
                _logger.LogWarning("Public profile not found for user {UserId}", id);
                return NotFound(res);
            }

            // Strip sensitive fields for non-owners/non-admins
            var currentUserId = ResolveUserId();
            var isAdmin = User.IsInRole("ADMIN");

            if ((currentUserId == null || currentUserId != id) && !isAdmin)
            {
                var publicDto = res.Data! with
                {
                    Email = string.Empty, // Hide email
                    Phone = null  // Hide phone
                };
                return Ok(ApiResponse<UserFullProfileDto>.SuccessResponse(publicDto));
            }

            return Ok(res);
        }

        /// <summary>
        /// Search users with pagination (Admin only)
        /// </summary>
        /// <param name="q">Search query (username, email, display name)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Paginated list of users matching search criteria</returns>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserReadDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchUsers(
            [FromQuery] string? q, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20, 
            CancellationToken ct = default)
        {
            _logger.LogInformation("Admin searching users: Query='{Query}', Page={Page}, PageSize={PageSize}", 
                q ?? "(all)", page, pageSize);
            
            var res = await _queries.Query(new SearchUsersQuery(q, page, pageSize), ct);
            
            return Ok(res);
        }

        /// <summary>
        /// Get user by username (Admin only)
        /// </summary>
        /// <param name="username">Username to search for</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>User information matching the username</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("by-username/{username}")]
        [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByUsername(string username, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("GetByUsername called with empty username");
                return BadRequest(ApiResponse<UserReadDto>.FailureResponse("Username cannot be empty", 400));
            }

            _logger.LogInformation("Admin retrieving user by username: {Username}", username);
            
            var res = await _queries.Query(new GetUserByUsernameQuery(username), ct);
            
            if (!res.Success)
            {
                _logger.LogWarning("User with username '{Username}' not found", username);
                return NotFound(res);
            }
            
            return Ok(res);
        }

        /// <summary>
        /// Get users with STAFF role (Admin/Staff only)
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Paginated list of staff accounts</returns>
        [Authorize(Roles = "ADMIN,STAFF")]
        [HttpGet("staff")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserReadDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStaffUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Retrieving STAFF accounts: Page={Page}, PageSize={PageSize}", page, pageSize);

            var res = await _queries.Query(new GetUsersByRoleQuery("STAFF", page, pageSize), ct);
            return Ok(res);
        }

        /// <summary>
        /// Get users with HOST role (Admin/Staff only)
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Paginated list of host accounts</returns>
        [Authorize(Roles = "ADMIN,STAFF")]
        [HttpGet("hosts")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserReadDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHostUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Retrieving HOST accounts: Page={Page}, PageSize={PageSize}", page, pageSize);

            var res = await _queries.Query(new GetUsersByRoleQuery("HOST", page, pageSize), ct);
            return Ok(res);
        }

        private Guid? ResolveUserId()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            
            if (Guid.TryParse(userIdStr, out var userId))
                return userId;
            
            return null;
        }
    }
}