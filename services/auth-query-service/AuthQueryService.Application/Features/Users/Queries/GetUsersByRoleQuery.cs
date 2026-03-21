using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;

namespace AuthQueryService.Application.Services.Users.Queries.GetUsersByRole
{
    public sealed record GetUsersByRoleQuery(
        string Role,
        int Page = 1,
        int PageSize = 20) : IQuery<PagedResult<UserReadDto>>;
}
