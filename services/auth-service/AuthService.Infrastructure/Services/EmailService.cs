using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Services
{
    public class EmailOptions
    {
        public string Host { get; set; } = null!;
        public int Port { get; set; }
        public string From { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class EmailService : IEmailService
    {
        private readonly EmailOptions _options;

        public EmailService(IOptions<EmailOptions> options)
        {
            _options = options.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var client = new SmtpClient(_options.Host, _options.Port)
            {
                Credentials = new NetworkCredential(_options.Username, _options.Password),
                EnableSsl = true
            };

            var mail = new MailMessage(_options.From, to, subject, body)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mail);
        }
    }
}

