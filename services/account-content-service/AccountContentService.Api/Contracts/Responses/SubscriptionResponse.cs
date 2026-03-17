namespace AccountContentService.Api.Contracts.Responses
{
    public class SubscriptionPlanResponse
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
    }
}
