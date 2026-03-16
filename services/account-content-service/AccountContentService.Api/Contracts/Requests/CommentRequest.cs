using System.ComponentModel.DataAnnotations;

namespace AccountContentService.Api.Contracts.Requests
{
    public class CommentRequest
    {
        public required string Content { get; set; }
    }

}
