using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Repositories
{
    public class OtpRepository : IOtpRepository
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AuthDbContext _context;

        public OtpRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _context = (AuthDbContext)_unitOfWork.Context;
        }

        public async Task<OtpCode?> GetByEmailAndCodeAsync(string email, string code, OtpPurpose purpose)
        {
            return await _context.OtpCodes
                .FirstOrDefaultAsync(otp => 
                    otp.Email == email && 
                    otp.Code == code && 
                    otp.Purpose == purpose &&
                    !otp.IsUsed &&
                    otp.ExpiresAt > DateTime.UtcNow);
        }

        public async Task<OtpCode?> GetLatestByEmailAsync(string email, OtpPurpose purpose)
        {
            return await _context.OtpCodes
                .Where(otp => otp.Email == email && otp.Purpose == purpose && !otp.IsUsed)
                .OrderByDescending(otp => otp.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(OtpCode otpCode)
        {
            await _context.OtpCodes.AddAsync(otpCode);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateAsync(OtpCode otpCode)
        {
            _context.OtpCodes.Update(otpCode);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task InvalidateAllForEmailAsync(string email, OtpPurpose purpose)
        {
            var otps = await _context.OtpCodes
                .Where(otp => otp.Email == email && otp.Purpose == purpose && !otp.IsUsed)
                .ToListAsync();

            foreach (var otp in otps)
            {
                otp.IsUsed = true;
                otp.UsedAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}

