using AuthService.Domain.Entities;
using System.Threading.Tasks;

namespace AuthService.Domain.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetByUsernameAsync(string name);
        Task<User?> LoginAsync(string email, string password);
        Task<User?> LoginByUsernameOrEmailAsync(string emailOrUsername, string password);
        Task<User?> GetByUsernameOrEmailAsync(string emailOrUsername);
        Task<User> RegisterAsync(string username, string email, string password, string firstName, string lastName);
        Task<Oauthaccount?> GetOAuthAccountAsync(string provider, string providerAccountId);
        Task AddOAuthAccountAsync(Oauthaccount oauthAccount);
        Task<User?> VerifyEmailAsync(string email, string otpCode);
    }
}