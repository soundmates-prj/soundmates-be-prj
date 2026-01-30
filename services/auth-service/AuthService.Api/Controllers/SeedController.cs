using AuthService.Api.Models.Responses;
using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.Features.Role.Commands;
using AuthService.Application.Features.Users.Commands;
using AuthService.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    /// <summary>
    /// Seed controller for initial data setup (Development/Migration only)
    /// Should be removed or disabled in production
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly ICommandDispatcher _commands;
        private readonly IRoleRepository _roleRepository;

        public SeedController(ICommandDispatcher commands, IRoleRepository roleRepository)
        {
            _commands = commands;
            _roleRepository = roleRepository;
        }

        /// <summary>
        /// Seed default roles and users for initial setup
        /// </summary>
        /// <remarks>
        /// Creates:
        /// - Roles: MEMBER, HOST, STAFF, ADMIN
        /// - Users: admin@server.com (ADMIN), host@server.com (HOST)
        /// Default password for both users: 123456
        /// </remarks>
        [HttpPost("default-users")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SeedDefaultUsers(CancellationToken ct)
        {
            var results = new List<object>();

            try
            {
                // Ensure MEMBER role exists
                var memberRole = await _roleRepository.GetByNameAsync("MEMBER");
                if (memberRole == null)
                {
                    var createMemberRoleCmd = new CreateRoleCommand { Name = "MEMBER" };
                    var memberRoleRes = await _commands.Send<CreateRoleCommand, Guid>(createMemberRoleCmd, ct);
                    if (memberRoleRes.IsSuccess)
                    {
                        memberRole = await _roleRepository.GetByIdAsync(memberRoleRes.Data);
                        results.Add(new { action = "Create MEMBER role", success = true, roleId = memberRoleRes.Data });
                    }
                    else
                    {
                        results.Add(new { action = "Create MEMBER role", success = false, error = memberRoleRes.ErrorMessage });
                    }
                }
                else
                {
                    results.Add(new { action = "MEMBER role exists", success = true, roleId = memberRole.Id });
                }

                // Ensure ADMIN role exists
                var adminRole = await _roleRepository.GetByNameAsync("ADMIN");
                if (adminRole == null)
                {
                    var createAdminRoleCmd = new CreateRoleCommand { Name = "ADMIN" };
                    var adminRoleRes = await _commands.Send<CreateRoleCommand, Guid>(createAdminRoleCmd, ct);
                    if (adminRoleRes.IsSuccess)
                    {
                        adminRole = await _roleRepository.GetByIdAsync(adminRoleRes.Data);
                        results.Add(new { action = "Create ADMIN role", success = true, roleId = adminRoleRes.Data });
                    }
                    else
                    {
                        results.Add(new { action = "Create ADMIN role", success = false, error = adminRoleRes.ErrorMessage });
                    }
                }
                else
                {
                    results.Add(new { action = "ADMIN role exists", success = true, roleId = adminRole.Id });
                }

                // Ensure HOST role exists
                var hostRole = await _roleRepository.GetByNameAsync("HOST");
                if (hostRole == null)
                {
                    var createHostRoleCmd = new CreateRoleCommand { Name = "HOST" };
                    var hostRoleRes = await _commands.Send<CreateRoleCommand, Guid>(createHostRoleCmd, ct);
                    if (hostRoleRes.IsSuccess)
                    {
                        hostRole = await _roleRepository.GetByIdAsync(hostRoleRes.Data);
                        results.Add(new { action = "Create HOST role", success = true, roleId = hostRoleRes.Data });
                    }
                    else
                    {
                        results.Add(new { action = "Create HOST role", success = false, error = hostRoleRes.ErrorMessage });
                    }
                }
                else
                {
                    results.Add(new { action = "HOST role exists", success = true, roleId = hostRole.Id });
                }

                // Ensure STAFF role exists
                var staffRole = await _roleRepository.GetByNameAsync("STAFF");
                if (staffRole == null)
                {
                    var createStaffRoleCmd = new CreateRoleCommand { Name = "STAFF" };
                    var staffRoleRes = await _commands.Send<CreateRoleCommand, Guid>(createStaffRoleCmd, ct);
                    if (staffRoleRes.IsSuccess)
                    {
                        staffRole = await _roleRepository.GetByIdAsync(staffRoleRes.Data);
                        results.Add(new { action = "Create STAFF role", success = true, roleId = staffRoleRes.Data });
                    }
                    else
                    {
                        results.Add(new { action = "Create STAFF role", success = false, error = staffRoleRes.ErrorMessage });
                    }
                }
                else
                {
                    results.Add(new { action = "STAFF role exists", success = true, roleId = staffRole.Id });
                }

                // Create admin user
                if (adminRole != null)
                {
                    var createAdminUserCmd = new CreateUserCommand
                    {
                        Username = "admin404",
                        Email = "admin@server.com",
                        FirstName = "System",
                        LastName = "Administrator",
                        Password = "123456",
                        RoleId = adminRole.Id
                    };
                    var adminUserRes = await _commands.Send<CreateUserCommand, Guid>(createAdminUserCmd, ct);
                    results.Add(new
                    {
                        action = "Create admin user",
                        success = adminUserRes.IsSuccess,
                        userId = adminUserRes.Data,
                        message = adminUserRes.ErrorMessage
                    });
                }

                // Create host user
                if (hostRole != null)
                {
                    var createHostUserCmd = new CreateUserCommand
                    {
                        Username = "host404",
                        Email = "host@server.com",
                        FirstName = "Default",
                        LastName = "Host",
                        Password = "123456",
                        RoleId = hostRole.Id
                    };
                    var hostUserRes = await _commands.Send<CreateUserCommand, Guid>(createHostUserCmd, ct);
                    results.Add(new
                    {
                        action = "Create host user",
                        success = hostUserRes.IsSuccess,
                        userId = hostUserRes.Data,
                        message = hostUserRes.ErrorMessage
                    });
                }

                return Ok(ApiResponse<object>.SuccessResponse(results, "Default users seeded successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.FailureResponse($"Error seeding default users: {ex.Message}", 500));
            }
        }
    }
}


