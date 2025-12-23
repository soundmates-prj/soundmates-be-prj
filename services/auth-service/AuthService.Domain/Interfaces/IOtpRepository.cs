using AuthService.Domain.Entities;
using System.Threading.Tasks;

namespace AuthService.Domain.Interfaces
{
    public interface IOtpRepository
    {
        Task<OtpCode?> GetByEmailAndCodeAsync(string email, string code, OtpPurpose purpose);
        Task<OtpCode?> GetLatestByEmailAsync(string email, OtpPurpose purpose);
        Task AddAsync(OtpCode otpCode);
        Task UpdateAsync(OtpCode otpCode);
        Task InvalidateAllForEmailAsync(string email, OtpPurpose purpose);
    }
}

