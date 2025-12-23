using AuthQueryService.Application.Abstractions.Messaging;

namespace AuthQueryService.Application.Users.Queries.GetUserById
{
    public sealed record GetUserByIdQuery(Guid Id) : IQuery<AuthQueryService.Application.DTOs.UserReadDto>;
}