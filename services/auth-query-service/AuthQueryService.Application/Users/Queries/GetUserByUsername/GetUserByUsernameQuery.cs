using AuthQueryService.Application.Abstractions.Messaging;

namespace AuthQueryService.Application.Users.Queries.GetUserByUsername
{
    public sealed record GetUserByUsernameQuery(string Username) : IQuery<AuthQueryService.Application.DTOs.UserReadDto>;
}