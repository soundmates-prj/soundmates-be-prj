using AccountContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.DTOs
{
    public class ReactionDto
    {
        public Guid Id { get; set; }

        public Guid PostId { get; set; }

        public Guid UserId { get; set; }

        public string ReactionType { get; set; } = "like";

        public DateTime CreatedAt { get; set; }
    }
}
