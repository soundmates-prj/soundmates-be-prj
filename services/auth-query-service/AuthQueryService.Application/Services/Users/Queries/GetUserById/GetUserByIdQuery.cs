using AuthQueryService.Application.Abstractions.Messaging;

namespace AuthQueryService.Application.Services.Users.Queries.GetUserById
{
    public sealed record GetUserByIdQuery(Guid Id) : IQuery<DTOs.UserReadDto>;
}