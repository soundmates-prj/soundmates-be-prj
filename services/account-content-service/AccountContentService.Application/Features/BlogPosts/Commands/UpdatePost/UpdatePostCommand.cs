using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Commands.UpdatePost
{
    public class UpdatePostCommand : IRequest<PostDto>
    {
        public Guid PostId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ContentText { get; set; } = string.Empty;

        public string? AudioUrl { get; set; }

        public string? ImageUrl { get; set; }

        public string? Status { get; set; } = "Edited";

        public string? PrivacyScope { get; set; }

        public string? MoodTag { get; set; }
    }
}
