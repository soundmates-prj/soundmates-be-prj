namespace AccountContentService.Api.Contracts.Requests
{
    public class ReactionRequest
    {
        public required Guid PostId { get; set; }

        public required Guid UserId { get; set; }

        public required string ReactionType { get; set; }
    }
}
