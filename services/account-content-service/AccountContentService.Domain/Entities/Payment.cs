namespace AccountContentService.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TargetType { get; set; } = string.Empty;

    public Guid TargetId { get; set; }

    public decimal TotalAmount { get; set; }

    public long? OrderCode { get; private set; } //PayOs

    public string Status { get; set; } = "pending";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? ExternalReference { get; set; }

    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();

    public void SetOrderCode(long orderCode)
    {
        OrderCode = orderCode;
    }
}