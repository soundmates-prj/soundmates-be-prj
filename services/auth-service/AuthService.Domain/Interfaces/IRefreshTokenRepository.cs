using AuthService.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace AuthService.Domain.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<RefreshToken?> GetByUserIdAsync(Guid userId);
        Task AddAsync(RefreshToken refreshToken);
        Task UpdateAsync(RefreshToken refreshToken);
        Task RevokeAllUserTokensAsync(Guid userId);
    }
}

