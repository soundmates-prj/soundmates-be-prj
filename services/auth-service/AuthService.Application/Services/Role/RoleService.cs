using AuthService.Application.DTOs;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Role.Interfaces;
using AuthService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _repo;
        public RoleService(IRoleRepository repo) => _repo = repo;

        public async Task<ApiResponse<RoleDto>> GetByIdAsync(Guid id)
        {
            var role = await _repo.GetByIdAsync(id);
            if (role == null) return ApiResponse<RoleDto>.FailureResponse("Not found", 404);
            return ApiResponse<RoleDto>.SuccessResponse(new RoleDto(role), "Get Role By Id Successfully!");
        }

        public async Task<ApiResponse<List<RoleDto>>> GetAllAsync()
        {
            var roles = await _repo.GetAllAsync();
            return ApiResponse<List<RoleDto>>.SuccessResponse(roles.Select(r => new RoleDto(r)).ToList(), "Get All Roles Successfully!");
        }

        public async Task<ApiResponse<RoleDto>> GetByNameAsync(string name)
        {
            var role = await _repo.GetByNameAsync(name);
            if (role == null) return ApiResponse<RoleDto>.FailureResponse("Not found", 404);
            return ApiResponse<RoleDto>.SuccessResponse(new RoleDto(role), "Get Role By Name Successfully!");
        }
    }
}
