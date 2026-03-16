using AccountContentService.Domain.Entities;

namespace AccountContentService.Api.Contracts.Responses
{
    public class ReactionResponse
    {
        public Guid Id { get; set; }

        public Guid PostId { get; set; }

        public Guid UserId { get; set; }

        public string ReactionType { get; set; } = "like";

        public DateTime CreatedAt { get; set; }
    }
}
