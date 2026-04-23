using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests.User;

public class UpdateBankAccountRequest
{
    [Required]
    public string BankId { get; set; } = string.Empty;
    
    [Required]
    public string AccountNumber { get; set; } = string.Empty;
    
    [Required]
    public string AccountName { get; set; } = string.Empty;
}
