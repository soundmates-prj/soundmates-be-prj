namespace AccountContentService.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid PlanId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime SubscribeAt { get; set; }

    public string Status { get; set; } = "active";

    public SubscriptionPlan Plan { get; set; } = null!;
}