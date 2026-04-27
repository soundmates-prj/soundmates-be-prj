using AccountContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.DTOs
{
    public class SubscriptionPlanDto
    {
        public Guid Id { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int DurationDays { get; set; }

        public int RequestLimit { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string? Description { get; set; }

        public int VoiceModelLimit { get; set; }

        public int TtsMinuteLimit { get; set; }

        public int PodcastRequestLimit { get; set; }
    }

    public class SubscriptionDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime SubscribeAt { get; set; }

        public string Status { get; set; } = "active";
        public UserProfileDto userProfile { get; set; } = null!;

        /// <summary>
        /// So luong gioc noi AI nguoi dung co the tao (clone)
        /// </summary>
        public int VoiceModelLimit { get; set; }

        /// <summary>
        /// So phut TTS moi thang
        /// </summary>
        public int TtsMinuteLimit { get; set; }

        /// <summary>
        /// So request podcast moi ngay
        /// </summary>
        public int PodcastRequestLimit { get; set; }

        public int RequestLimit { get; set; }
    }
}
