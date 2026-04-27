namespace AccountContentService.Api.Contracts.Requests
{
    public class SubscriptionPlanRequest
    {
        public required string PlanName { get; set; }

        public required decimal Price { get; set; }

        public required int DurationDays { get; set; }

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

        public string? Description { get; set; }
    }
}
