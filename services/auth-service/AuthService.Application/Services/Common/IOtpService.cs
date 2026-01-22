using AuthService.Domain.Entities;

namespace AuthService.Application.Services.Common;

public interface IOtpService
{
    Task<string> GenerateAndSendOtpAsync(
        string email, 
        string userName, 
        string? firstName, 
        OtpPurpose purpose,
        CancellationToken cancellationToken = default);
}
