using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.DTOs
{
    public class PostDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ContentText { get; set; } = string.Empty;

        public string? AudioUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public string? PrivacyScope { get; set; } = "Public";

        public string? MoodTag { get; set; }

        public string Status { get; set; } = "Draft";

        public bool IsGenerated { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
