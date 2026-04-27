namespace LiveSessionService.Application.Abstractions;

public interface IAccountContentClient
{
    // Fetches the subscription details of the current user, or null if no active subscription.
    // The userToken should be the raw JWT token string (e.g. "Bearer ...").
    Task<UserSubscriptionDto?> GetMySubscriptionFullAsync(string userToken, CancellationToken ct = default);
}

public class UserSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int RequestLimit { get; set; }
    public int VoiceModelLimit { get; set; }
    public int TtsMinuteLimit { get; set; }
    public int PodcastRequestLimit { get; set; }
    public string Status { get; set; } = string.Empty;
}
