using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Domain.Interfaces;

namespace AuthQueryService.Application.Services.Roles.Queries.GetAllRoles
{
    public sealed class GetAllRolesQueryHandler : IQueryHandler<GetAllRolesQuery, List<RoleDto>>
    {
        private readonly IRoleRepository _repository;

        public GetAllRolesQueryHandler(IRoleRepository repository) => _repository = repository;

        public async Task<ApiResponse<List<RoleDto>>> Handle(GetAllRolesQuery query, CancellationToken cancellationToken)
        {
            var roles = await _repository.GetAllAsync();
            var dtos = roles.Select(r => new RoleDto(r)).ToList();
            return ApiResponse<List<RoleDto>>.SuccessResponse(dtos);
        }
    }
}

