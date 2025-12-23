using System.Threading.Tasks;

namespace AuthService.Domain.Interfaces
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo?> VerifyGoogleTokenAsync(string idToken);
    }

    public class GoogleUserInfo
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Picture { get; set; }
        public bool EmailVerified { get; set; }
    }
}

