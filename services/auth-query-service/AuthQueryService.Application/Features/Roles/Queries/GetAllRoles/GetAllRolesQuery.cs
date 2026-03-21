using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;

namespace AuthQueryService.Application.Services.Roles.Queries.GetAllRoles
{
    public sealed record GetAllRolesQuery : IQuery<List<RoleDto>>;
}

