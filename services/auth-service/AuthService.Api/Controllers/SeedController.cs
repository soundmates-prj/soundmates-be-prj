using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.DTOs;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Role.Commands;
using AuthService.Application.Services.Users.Commands;
using AuthService.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
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

        // POST: /api/v1/seed/default-users
        [HttpPost("default-users")]
        public async Task<IActionResult> SeedDefaultUsers(CancellationToken ct)
        {
            var results = new List<object>();

            try
            {
                // Get or create ADMIN role
                var adminRole = await _roleRepository.GetByNameAsync("ADMIN");
                if (adminRole == null)
                {
                    var createAdminRoleCmd = new CreateRoleCommand { Name = "ADMIN" };
                    var adminRoleRes = await _commands.Send<CreateRoleCommand, Guid>(createAdminRoleCmd, ct);
                    if (adminRoleRes.Success)
                    {
                        adminRole = await _roleRepository.GetByIdAsync(adminRoleRes.Data);
                        results.Add(new { action = "Create ADMIN role", success = true, roleId = adminRoleRes.Data });
                    }
                    else
                    {
                        results.Add(new { action = "Create ADMIN role", success = false, error = adminRoleRes.Message });
                    }
                }
                else
                {
                    results.Add(new { action = "ADMIN role exists", success = true, roleId = adminRole.Id });
                }

                // Get or create HOST role
                var hostRole = await _roleRepository.GetByNameAsync("HOST");
                if (hostRole == null)
                {
                    var createHostRoleCmd = new CreateRoleCommand { Name = "HOST" };
                    var hostRoleRes = await _commands.Send<CreateRoleCommand, Guid>(createHostRoleCmd, ct);
                    if (hostRoleRes.Success)
                    {
                        hostRole = await _roleRepository.GetByIdAsync(hostRoleRes.Data);
                        results.Add(new { action = "Create HOST role", success = true, roleId = hostRoleRes.Data });
                    }
                    else
                    {
                        results.Add(new { action = "Create HOST role", success = false, error = hostRoleRes.Message });
                    }
                }
                else
                {
                    results.Add(new { action = "HOST role exists", success = true, roleId = hostRole.Id });
                }

                // Create admin user (admin@server.com, username: admin404, password: 123456)
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
                    results.Add(new { 
                        action = "Create admin user", 
                        success = adminUserRes.Success, 
                        userId = adminUserRes.Data,
                        message = adminUserRes.Message 
                    });
                }

                // Create host user (host@server.com, username: host404, password: 123456)
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
                    results.Add(new { 
                        action = "Create host user", 
                        success = hostUserRes.Success, 
                        userId = hostUserRes.Data,
                        message = hostUserRes.Message 
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

