using AccountContentService.Domain.Entities;

namespace AccountContentService.Api.Contracts.Responses
{
    public class ReactionResponse
    {
        public Guid Id { get; set; }

        public Guid PostId { get; set; }

        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserAvatarUrl { get; set; } = string.Empty;

        public string ReactionType { get; set; }

        public DateTime CreatedAt { get; set; }

    }
}
