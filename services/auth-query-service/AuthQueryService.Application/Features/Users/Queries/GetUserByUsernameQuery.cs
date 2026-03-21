using AuthQueryService.Application.Abstractions.Messaging;

namespace AuthQueryService.Application.Services.Users.Queries.GetUserByUsername
{
    public sealed record GetUserByUsernameQuery(string Username) : IQuery<DTOs.UserReadDto>;
}