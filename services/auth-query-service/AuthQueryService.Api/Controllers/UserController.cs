using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Application.Users.Queries.GetUserById;
using AuthQueryService.Application.Users.Queries.GetUserByUsername;
using AuthQueryService.Application.Users.Queries.SearchUsers;
using AuthQueryService.Application.Users.Queries.GetFullUserProfile;
using AuthQueryService.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace AuthQueryService.Api.Controllers
{
    [Route("api/v1/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        // Command Dispatcher (Commands)
        // RabbitMQ Publisher
        private readonly IMessageBusPublisher _messageBusPublisher;
        // Query Dispatcher (Queries)
        private readonly IQueryDispatcher _queries;

        public UserController(IMessageBusPublisher messageBusPublisher, IQueryDispatcher queries)
        {
            _messageBusPublisher = messageBusPublisher;
            _queries = queries;
        }

        // GET: /api/v1/users/me/profile - Get current user's basic profile
        [Authorize]
        [HttpGet("me/profile")]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        {
            // JWT token uses "sub" (JwtRegisteredClaimNames.Sub) for user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) 
                           ?? User.FindFirst("sub") 
                           ?? User.Claims.FirstOrDefault(c => c.Type == "user_id");
            
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(ApiResponse<UserReadDto>.FailureResponse("Invalid or missing user token", 401));
            }
            var res = await _queries.Query(new GetUserByIdQuery(userId), ct);
            if (!res.Success) 
                return NotFound(res);
            return Ok(res);
        }

        // GET: /api/v1/users/me/profile/full - Get current user's full profile
        [Authorize]
        [HttpGet("me/profile/full")]
        public async Task<IActionResult> GetMyFullProfile(CancellationToken ct)
        {
            // JWT token uses "sub" (JwtRegisteredClaimNames.Sub) for user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) 
                           ?? User.FindFirst("sub") 
                           ?? User.Claims.FirstOrDefault(c => c.Type == "user_id");
            
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(ApiResponse<UserFullProfileDto>.FailureResponse("Invalid or missing user token", 401));
            }
            var res = await _queries.Query(new GetFullUserProfileQuery(userId), ct);
            if (!res.Success) 
                return NotFound(res);
            return Ok(res);
        }

        // GET: /api/v1/users/{id} - Get user by ID
        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var res = await _queries.Query(new GetUserByIdQuery(id), ct);
            if (!res.Success) 
                return NotFound(res);
            return Ok(res);
        }

        // GET: /api/v1/users/{id}/profile - Get user's basic profile by ID
        [Authorize]
        [HttpGet("{id:guid}/profile")]
        public async Task<IActionResult> GetProfileById(Guid id, CancellationToken ct)
        {
            var res = await _queries.Query(new GetUserByIdQuery(id), ct);
            if (!res.Success) 
                return NotFound(res);
            return Ok(res);
        }

        // GET: /api/v1/users/{id}/profile/full - Get user's full profile by ID
        [Authorize]
        [HttpGet("{id:guid}/profile/full")]
        public async Task<IActionResult> GetFullProfileById(Guid id, CancellationToken ct)
        {
            var res = await _queries.Query(new GetFullUserProfileQuery(id), ct);
            if (!res.Success) 
                return NotFound(res);
            return Ok(res);
        }

        // GET: /api/v1/users?q=abc&page=1&pageSize=20 - Search users
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SearchUsers([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var res = await _queries.Query(new SearchUsersQuery(q, page, pageSize), ct);
            return Ok(res);
        }

        // GET: /api/v1/users/by-username/{username} - Get user by username
        [Authorize]
        [HttpGet("by-username/{username}")]
        public async Task<IActionResult> GetByUsername(string username, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(ApiResponse<UserReadDto>.FailureResponse("Username cannot be empty", 400));
            }
            var res = await _queries.Query(new GetUserByUsernameQuery(username), ct);
            if (!res.Success) 
                return NotFound(res);
            return Ok(res);
        }
    }
}