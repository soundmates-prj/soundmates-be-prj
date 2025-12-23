using AuthService.Domain.Interfaces;
using AuthService.Domain.Entities;
using System.Threading.Tasks;
using AuthService.Infrastructure.Dao.Interfaces;

namespace AuthService.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IAuthDao _authDao;

        public AuthRepository(IAuthDao authDao)
        {
            _authDao = authDao;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            return await _authDao.LoginAsync(email, password);
        }

        public async Task<User?> LoginByUsernameOrEmailAsync(string emailOrUsername, string password)
        {
            return await _authDao.LoginByUsernameOrEmailAsync(emailOrUsername, password);
        }

        public async Task<User> RegisterAsync(string username, string email, string password, string firstName, string lastName)
        {
            return await _authDao.RegisterAsync(username, email, password, firstName, lastName);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _authDao.GetByUsernameAsync(username);
        }

        public async Task<Oauthaccount?> GetOAuthAccountAsync(string provider, string providerAccountId)
        {
            return await _authDao.GetOAuthAccountAsync(provider, providerAccountId);
        }

        public async Task AddOAuthAccountAsync(Oauthaccount oauthAccount)
        {
            await _authDao.AddOAuthAccountAsync(oauthAccount);
        }

        public async Task<User?> VerifyEmailAsync(string email, string otpCode)
        {
            return await _authDao.VerifyEmailAsync(email, otpCode);
        }
    }
}
