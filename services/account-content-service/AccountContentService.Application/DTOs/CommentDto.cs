using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.DTOs
{
    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid ParentCommentId { get; set; }

        public Guid PostId { get; set; }

        public Guid UserId { get; set; }


        public string Content { get; set; } = string.Empty;

        public string Status { get; set; } = "active";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
        public List<CommentDto> Replies { get; set; } = new();
    }
}
