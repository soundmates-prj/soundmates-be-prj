using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;

namespace AuthQueryService.Application.Services.Roles.Queries.SearchRoles
{
    public sealed record SearchRolesQuery(string? Q, int Page = 1, int PageSize = 20) : IQuery<PagedResult<RoleDto>>;
}

