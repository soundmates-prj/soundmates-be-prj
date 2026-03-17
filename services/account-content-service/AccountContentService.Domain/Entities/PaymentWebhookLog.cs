namespace AccountContentService.Domain.Entities;

public class PaymentWebhookLog
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;

    public bool Processed { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid TransactionId { get; set; }
}