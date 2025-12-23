using AuthService.Application.DTOs;
using AuthService.Application.DTOs.Response;
using AuthService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Services.Role.Interfaces
{
    public interface IRoleService
    {
        Task<ApiResponse<RoleDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<RoleDto>> GetByNameAsync(string name);
        Task<ApiResponse<List<RoleDto>>> GetAllAsync();
    }
}
