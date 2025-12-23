using AuthQueryService.Application.Abstractions.Messaging;

namespace AuthQueryService.Application.Users.Queries.GetFullUserProfile
{
    public sealed record GetFullUserProfileQuery(Guid UserId) : IQuery<AuthQueryService.Application.DTOs.UserFullProfileDto>;
}
