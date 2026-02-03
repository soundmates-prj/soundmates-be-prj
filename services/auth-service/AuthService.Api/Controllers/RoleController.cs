using AuthService.Api.Models.Requests.Role;
using AuthService.Api.Models.Responses;
using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.Features.Role.Commands;
using AuthService.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    [Route("api/v1/roles")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class RoleController : ControllerBase
    {
        private readonly ICommandDispatcher _commands;
        private readonly IOutboxRepository _outbox;
        private readonly IUnitOfWork _unitOfWork;

        public RoleController(
            ICommandDispatcher commands, 
            IOutboxRepository outbox,
            IUnitOfWork unitOfWork)
        {
            _commands = commands;
            _outbox = outbox;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Create a new role
        /// </summary>
        /// <remarks>
        /// Only ADMIN can create roles
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<Guid>.FailureResponse("Invalid input", 400));

            var cmd = new CreateRoleCommand
            {
                Name = request.Name
            };

            var result = await _commands.Send<CreateRoleCommand, Guid>(cmd, ct);
            
            if (!result.IsSuccess)
                return BadRequest(ApiResponse<Guid>.FailureResponse(result.ErrorMessage ?? "Failed to create role", result.ErrorCode ?? 400));

            return StatusCode(StatusCodes.Status201Created, 
                ApiResponse<Guid>.SuccessResponse(result.Data, "Role created successfully"));
        }

        /// <summary>
        /// Update an existing role
        /// </summary>
        /// <remarks>
        /// Only ADMIN can update roles
        /// </remarks>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

            var cmd = new UpdateRoleCommand(id)
            {
                Name = request.Name
            };

            var result = await _commands.Send<UpdateRoleCommand, bool>(cmd, ct);
            
            if (!result.IsSuccess)
                return NotFound(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Role not found", 404));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Role updated successfully"));
        }

        /// <summary>
        /// Delete a role
        /// </summary>
        /// <remarks>
        /// Only ADMIN can delete roles. Cannot delete if role is assigned to users.
        /// </remarks>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var cmd = new DeleteRoleCommand(id);
            var result = await _commands.Send<DeleteRoleCommand, bool>(cmd, ct);
            
            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    404 => NotFound(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Role not found", 404)),
                    _ => BadRequest(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Failed to delete role", result.ErrorCode ?? 400))
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Sync all roles to query service (Development/Migration only)
        /// </summary>
        /// <remarks>
        /// This endpoint should be removed after deployment/migration is complete
        /// </remarks>
        [HttpPost("sync")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SyncRoles([FromServices] IRoleRepository roleRepository, CancellationToken ct)
        {
            var roles = await roleRepository.GetAllAsync();
            if (roles == null || !roles.Any())
            {
                return Ok(ApiResponse<object>.SuccessResponse(
                    new { count = 0 }, 
                    "No roles to sync"));
            }

            var synced = 0;
            foreach (var role in roles)
            {
                await _outbox.EnqueueAsync("auth.role.created", new
                {
                    id = role.Id,
                    name = role.Name
                }, ct);
                synced++;
            }
            
            await _unitOfWork.SaveChangesAsync(ct);

            return Ok(ApiResponse<object>.SuccessResponse(
                new { count = synced }, 
                $"Synced {synced} roles to query service"));
        }
    }
}

