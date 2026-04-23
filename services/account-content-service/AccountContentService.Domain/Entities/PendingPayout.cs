using AccountContentService.Domain.Enums;

namespace AccountContentService.Domain.Entities;

public class PendingPayout
{
    public Guid Id { get; set; }
    
    public Guid PaymentId { get; set; }
    
    public Guid TargetUserId { get; set; }
    
    public decimal Amount { get; set; }
    
    public string? BankId { get; set; }
    
    public string? AccountNumber { get; set; }
    
    public string? AccountName { get; set; }
    
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed
    
    public DateTime ScheduledAt { get; set; }
    
    public DateTime? ExecutedAt { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
