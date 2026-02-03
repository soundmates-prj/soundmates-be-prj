using AuthService.Application.Features.Common;
using AuthService.Domain.Interfaces;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// BCrypt implementation of password hasher
/// This belongs in INFRASTRUCTURE layer - external library concern
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string plainPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainPassword);
    }

    public bool VerifyPassword(string plainPassword, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
    }
}
