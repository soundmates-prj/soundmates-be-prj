using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Domain.Interfaces;

namespace AuthQueryService.Application.Roles.Queries.GetRoleByName
{
    public sealed class GetRoleByNameQueryHandler : IQueryHandler<GetRoleByNameQuery, RoleDto>
    {
        private readonly IRoleRepository _repository;

        public GetRoleByNameQueryHandler(IRoleRepository repository) => _repository = repository;

        public async Task<ApiResponse<RoleDto>> Handle(GetRoleByNameQuery query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query.Name))
            {
                return ApiResponse<RoleDto>.FailureResponse("Role name cannot be empty", 400);
            }

            var role = await _repository.GetByNameAsync(query.Name);
            if (role is null)
                return ApiResponse<RoleDto>.FailureResponse("Role not found", 404);

            return ApiResponse<RoleDto>.SuccessResponse(new RoleDto(role));
        }
    }
}

