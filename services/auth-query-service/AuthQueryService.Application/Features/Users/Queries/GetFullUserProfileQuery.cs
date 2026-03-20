using AuthQueryService.Application.Abstractions.Messaging;

namespace AuthQueryService.Application.Services.Users.Queries.GetFullUserProfile
{
    public sealed record GetFullUserProfileQuery(Guid UserId) : IQuery<DTOs.UserFullProfileDto>;
}
