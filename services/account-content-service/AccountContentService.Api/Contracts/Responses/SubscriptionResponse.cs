namespace AccountContentService.Api.Contracts.Responses
{
    public class SubscriptionPlanResponse
    {
        public Guid Id { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int DurationDays { get; set; }

        public int RequestLimit { get; set; }

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

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string? Description { get; set; }
    }

    public class SubscriptionResponse
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid PlanId { get; set; }

        public string PlanName { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime SubscribeAt { get; set; }

        public string Status { get; set; }

        public UserProfileResponse userProfile { get; set; } = null!;
    }
}
