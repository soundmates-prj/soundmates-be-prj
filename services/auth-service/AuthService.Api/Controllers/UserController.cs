using AuthService.Api.Models.Requests.User;
using AuthService.Api.Models.Responses;
using AuthService.Api.Extensions;
using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.Features.Users.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    /// <summary>
    /// CRUD user account management endpoints.
    /// </summary>
    [Route("api/v1/users")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class UserController : ControllerBase
    {
        private readonly ICommandDispatcher _commands;

        public UserController(ICommandDispatcher commands)
        {
            _commands = commands;
        }

        /// <summary>
        /// Create a new user (Admin only)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<Guid>.FailureResponse("Invalid input", 400));

            var cmd = new CreateUserCommand
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                RoleId = request.RoleId,
                Password = request.Password
            };

            var result = await _commands.Send<CreateUserCommand, Guid>(cmd, ct);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<Guid>.FailureResponse(result.ErrorMessage ?? "Failed to create user", result.ErrorCode ?? 400));

            return StatusCode(StatusCodes.Status201Created, 
                ApiResponse<Guid>.SuccessResponse(result.Data, "User created successfully"));
        }

        /// <summary>
        /// Update user information (Admin only)
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

            var cmd = new UpdateUserCommand(id)
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                RoleId = request.RoleId
            };

            var result = await _commands.Send<UpdateUserCommand, bool>(cmd, ct);

            if (!result.IsSuccess)
                return NotFound(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "User not found", 404));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "User updated successfully"));
        }

        /// <summary>
        /// Ban a user account (Admin only)
        /// </summary>
        [HttpPost("{id:guid}/ban")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Ban(Guid id, [FromBody] BanUserRequest? request, CancellationToken ct)
        {
            var cmd = new BanUserCommand(id, request?.Reason);
            var result = await _commands.Send<BanUserCommand, bool>(cmd, ct);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Failed to ban user", result.ErrorCode ?? 400));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "User banned successfully"));
        }

        /// <summary>
        /// Unban a user account (Admin only)
        /// </summary>
        [HttpPost("{id:guid}/unban")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Unban(Guid id, CancellationToken ct)
        {
            var cmd = new UnbanUserCommand(id);
            var result = await _commands.Send<UnbanUserCommand, bool>(cmd, ct);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Failed to unban user", result.ErrorCode ?? 400));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "User unbanned successfully"));
        }

        /// <summary>
        /// Deactivate user account (Soft delete)
        /// </summary>
        /// <remarks>
        /// Any authenticated user can deactivate their own account.
        /// Admin can deactivate any user account.
        /// </remarks>
        [HttpPost("{id:guid}/deactivate")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        {
            // Get current user ID from JWT
            if (!User.TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid or missing user token", 401));
            }

            // Check if user is admin or deactivating their own account
            var isAdmin = User.IsInRole("ADMIN");
            if (!isAdmin && currentUserId != id)
            {
                return StatusCode(StatusCodes.Status403Forbidden, 
                    ApiResponse<bool>.FailureResponse("You can only deactivate your own account", 403));
            }

            var cmd = new DeactivateUserCommand(id);
            var result = await _commands.Send<DeactivateUserCommand, bool>(cmd, ct);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Failed to deactivate user", result.ErrorCode ?? 400));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "User account deactivated successfully"));
        }

        /// <summary>
        /// Delete user account (Hard delete - Use with caution!)
        /// </summary>
        /// <remarks>
        /// WARNING: This permanently deletes the user. Consider using Deactivate instead.
        /// Only ADMIN can perform hard delete.
        /// </remarks>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var cmd = new DeleteUserCommand(id);
            var result = await _commands.Send<DeleteUserCommand, bool>(cmd, ct);

            if (!result.IsSuccess)
                return NotFound(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "User not found", 404));

            return NoContent();
        }
    }
}
