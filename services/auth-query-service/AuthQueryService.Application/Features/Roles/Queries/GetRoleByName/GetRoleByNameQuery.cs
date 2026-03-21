using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;

namespace AuthQueryService.Application.Services.Roles.Queries.GetRoleByName
{
    public sealed record GetRoleByNameQuery(string Name) : IQuery<RoleDto>;
}

