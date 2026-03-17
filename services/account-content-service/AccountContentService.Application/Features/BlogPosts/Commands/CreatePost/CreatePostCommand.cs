using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Commands.CreatePost
{
    public class CreatePostCommand : IRequest<PostDto>
    {
        public Guid UserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ContentText { get; set; } = string.Empty;

        public string? AudioUrl { get; set; }

        public string? Status { get; set; } = "Draft";

        public string? PrivacyScope { get; set; }

        public string? MoodTag { get; set; }
    }
}
