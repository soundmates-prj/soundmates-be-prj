namespace AccountContentService.Api.Contracts.Requests
{
    public class SubscriptionPlanRequest
    {
        public required string PlanName { get; set; }

        public required decimal Price { get; set; }

        public required int DurationDays { get; set; }

        public int RequestLimit { get; set; }

        public string? Description { get; set; }
    }
}
