using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Domain.Interfaces;

namespace AuthQueryService.Application.Users.Queries.GetUserRole
{
    public sealed class GetUserRoleQueryHandler : IQueryHandler<GetUserRoleQuery, RoleDto>
    {
        private readonly IUserReadRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public GetUserRoleQueryHandler(IUserReadRepository userRepository, IRoleRepository roleRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task<ApiResponse<RoleDto>> Handle(GetUserRoleQuery query, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(query.UserId);
            if (user is null)
                return ApiResponse<RoleDto>.FailureResponse("User not found", 404);

            if (!user.RoleId.HasValue)
                return ApiResponse<RoleDto>.FailureResponse("User has no role assigned", 404);

            var role = await _roleRepository.GetByIdAsync(user.RoleId.Value);
            if (role is null)
                return ApiResponse<RoleDto>.FailureResponse("Role not found", 404);

            return ApiResponse<RoleDto>.SuccessResponse(new RoleDto(role));
        }
    }
}

