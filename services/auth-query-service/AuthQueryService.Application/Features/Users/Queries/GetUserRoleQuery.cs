using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;

namespace AuthQueryService.Application.Services.Users.Queries.GetUserRole
{
    public sealed record GetUserRoleQuery(Guid UserId) : IQuery<RoleDto>;
}

