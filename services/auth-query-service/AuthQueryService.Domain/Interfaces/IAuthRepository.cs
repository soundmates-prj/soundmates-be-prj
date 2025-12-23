using AuthQueryService.Domain.Entities;
using System.Threading.Tasks;

namespace AuthQueryService.Domain.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetByUsernameAsync(string name);
        Task<User?> LoginAsync(string username, string password);
        Task<User?> RegisterAsync(string username, string email, string password, string fullName);
    }
}