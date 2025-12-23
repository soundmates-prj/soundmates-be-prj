using AuthService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Dao.Interfaces
{
    public interface IAuthDao
    {
        Task<User?> LoginAsync(string email, string password);
        Task<User?> LoginByUsernameOrEmailAsync(string emailOrUsername, string password);
        Task<User> RegisterAsync(string username, string email, string password, string firstName, string lastName);
        Task<User?> GetByUsernameAsync(string username);
        Task<Oauthaccount?> GetOAuthAccountAsync(string provider, string providerAccountId);
        Task AddOAuthAccountAsync(Oauthaccount oauthAccount);
        Task<User?> VerifyEmailAsync(string email, string otpCode);
    }
}
