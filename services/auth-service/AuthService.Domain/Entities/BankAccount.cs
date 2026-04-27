using System;

namespace AuthService.Domain.Entities;

public class BankAccount
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    public string BankId { get; set; } = null!;
    
    public string AccountNumber { get; set; } = null!;
    
    public string AccountName { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
