using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;

namespace AuthQueryService.Application.Services.Roles.Queries.GetRoleById
{
    public sealed record GetRoleByIdQuery(Guid Id) : IQuery<RoleDto>;
}

