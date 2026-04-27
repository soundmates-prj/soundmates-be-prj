namespace AccountContentService.Domain.Entities;

public class SubscriptionPlan
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

    /// <summary>
    /// So luong gioc noi AI nguoi dung co the tao (clone)
    /// </summary>
    public int VoiceModelLimit { get; set; } = 0;

    /// <summary>
    /// So phut TTS moi thang (tong thoi gian audio TTS duoc phep)
    /// </summary>
    public int TtsMinuteLimit { get; set; } = 0;

    /// <summary>
    /// So request podcast moi ngay
    /// </summary>
    public int PodcastRequestLimit { get; set; } = 0;

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}