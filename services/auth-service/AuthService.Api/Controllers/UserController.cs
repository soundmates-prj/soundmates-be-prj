using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.DTOs;
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

        // Delete Account Method
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