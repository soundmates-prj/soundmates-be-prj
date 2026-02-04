using AuthService.Domain.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(
            IConfiguration configuration,
            ILogger<GoogleAuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<GoogleUserInfo?> VerifyGoogleTokenAsync(string idToken)
        {
            try
            {
                var clientId = _configuration["GoogleOAuth:ClientId"];
                
                // Debug logging
                _logger.LogInformation("Verifying Google token...");
                _logger.LogInformation("Google ClientId configured: {ClientIdExists}", !string.IsNullOrEmpty(clientId));
                _logger.LogInformation("ClientId value (first 10 chars): {ClientIdPrefix}", 
                    string.IsNullOrEmpty(clientId) ? "NULL" : clientId.Substring(0, Math.Min(10, clientId.Length)));
                
                if (string.IsNullOrEmpty(clientId))
                {
                    _logger.LogError("Google OAuth ClientId is not configured in appsettings or environment");
                    throw new InvalidOperationException("Google OAuth ClientId is not configured");
                }

                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                };

                _logger.LogInformation("Calling Google token validation...");
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                
                _logger.LogInformation("Google token validated successfully for email: {Email}", payload.Email);

                return new GoogleUserInfo
                {
                    Id = payload.Subject,
                    Email = payload.Email,
                    Name = payload.Name,
                    Picture = payload.Picture,
                    EmailVerified = payload.EmailVerified
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify Google token: {Message}", ex.Message);
                return null;
            }
        }
    }
}

