using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.DTOs;
using AuthService.Application.DTOs.Request;
using AuthService.Application.Services.Users.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    [Route("api/v1/users")]
    [ApiController]
    // Only ADMIN can create/update/delete other users
    [Authorize(Roles = "ADMIN")]
    public class UserController : ControllerBase
    {
        private readonly ICommandDispatcher _commands;

        public UserController(ICommandDispatcher commands)
        {
            _commands = commands;
        }

        // Create Account Method
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserDto dto, CancellationToken ct)
        {
            // Mapping from DTO into Command
            var cmd = new CreateUserCommand
            {
                Username = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                RoleId = dto.RoleId,
                Password = dto.Password ?? string.Empty
            };

            var res = await _commands.Send<CreateUserCommand, Guid>(cmd, ct);

            // Handle Errors
            if (!res.Success) 
                return BadRequest(res);

            // Note: Event is already published by CreateUserHandler via outbox pattern
            // Return ApiResponse with Guid data (id only)
            return StatusCode(StatusCodes.Status201Created, res);
        }

        // Update Account Method
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UserDto dto, CancellationToken ct)
        {
            // Mapping from DTO into Command
            var cmd = new UpdateUserCommand(id)
            {
                Username = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                RoleId = dto.RoleId
            };

            var res = await _commands.Send<UpdateUserCommand, bool>(cmd, ct);

            // Handle Errors
            if (!res.Success) 
                return NotFound(res);

            // Note: Event is already published by UpdateUserHandler via outbox pattern
            // Return Response
            return Ok(res);
        }

        // Ban User Account - Set IsActive = false (Admin action)
        [HttpPost("{id:guid}/ban")]
        public async Task<IActionResult> Ban(Guid id, [FromBody] BanUserRequest? request, CancellationToken ct)
        {
            var cmd = new BanUserCommand(id, request?.Reason);
            var res = await _commands.Send<BanUserCommand, bool>(cmd, ct);

            if (!res.Success)
                return BadRequest(res);

            // Note: Event 'auth.user.banned' is published by BanUserHandler
            return Ok(res);
        }

        // Unban User Account - Set IsActive = true (Admin action)
        [HttpPost("{id:guid}/unban")]
        public async Task<IActionResult> Unban(Guid id, CancellationToken ct)
        {
            var cmd = new UnbanUserCommand(id);
            var res = await _commands.Send<UnbanUserCommand, bool>(cmd, ct);

            if (!res.Success)
                return BadRequest(res);

            // Note: Event 'auth.user.unbanned' is published by UnbanUserHandler
            return Ok(res);
        }

        // Deactivate Account - Soft delete (User or Admin action)
        // This is the RECOMMENDED approach instead of hard DELETE
        [HttpPost("{id:guid}/deactivate")]
        [Authorize] // Any authenticated user can deactivate their own account
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        {
            // TODO: Add authorization check to ensure user can only deactivate their own account
            // unless they are ADMIN
            
            var cmd = new DeactivateUserCommand(id);
            var res = await _commands.Send<DeactivateUserCommand, bool>(cmd, ct);

            if (!res.Success)
                return BadRequest(res);

            // Note: Event 'auth.user.deactivated' is published by DeactivateUserHandler
            return Ok(res);
        }

        // Delete Account Method - HARD DELETE (Use with caution!)
        // WARNING: This permanently deletes the user. Consider using Deactivate instead.
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            // Command side only: delete in auth-service-write
            var cmd = new DeleteUserCommand(id);
            
            var res = await _commands.Send<DeleteUserCommand, bool>(cmd, ct);
            
            if(!res.Success)
                return NotFound(res);

            // Note: Event is already published by DeleteUserHandler via outbox pattern
            return NoContent();
        }
    }
}