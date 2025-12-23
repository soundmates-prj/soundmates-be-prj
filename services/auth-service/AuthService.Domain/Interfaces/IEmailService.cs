using System.Threading.Tasks;

namespace AuthService.Domain.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}

