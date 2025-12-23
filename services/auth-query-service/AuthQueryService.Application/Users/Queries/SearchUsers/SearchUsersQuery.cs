using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;

namespace AuthQueryService.Application.Users.Queries.SearchUsers
{
    public sealed record SearchUsersQuery(
        string? Q,
        int Page = 1,
        int PageSize = 20) : IQuery<PagedResult<UserReadDto>>;
}