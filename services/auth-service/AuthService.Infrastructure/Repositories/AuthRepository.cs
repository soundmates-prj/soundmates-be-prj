using AuthService.Application.Enums;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

/// <summary>
/// Auth repository implementation
/// Direct access to DbContext through UnitOfWork (DAO layer removed)
/// </summary>
public class AuthRepository : IAuthRepository
{
    private readonly IUnitOfWork _uow;
    private readonly AuthDbContext _db;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthRepository(IUnitOfWork uow, IDateTimeProvider dateTimeProvider)
    {
        _uow = uow;
        _db = (AuthDbContext)_uow.Context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        // Fetch user by email (password is verified with BCrypt)
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (user is null) return null;

        // Verify password with BCrypt (hash comparison)
        var ok = BCrypt.Net.BCrypt.Verify(password, user.Password);
        return ok ? user : null;
    }

    public async Task<User?> LoginByUsernameOrEmailAsync(string emailOrUsername, string password)
    {
        // Try to find user by email or username
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == emailOrUsername.ToLower() ||
                u.Username.ToLower() == emailOrUsername.ToLower());

        if (user is null) return null;

        // Verify password with BCrypt (hash comparison)
        var ok = BCrypt.Net.BCrypt.Verify(password, user.Password);
        return ok ? user : null;
    }

    public async Task<User> RegisterAsync(string username, string email, string password, string firstName, string lastName)
    {
        // Check for existing username
        var existingByUsername = await _db.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (existingByUsername != null)
        {
            throw new Application.Exceptions.AuthException(
                AuthErrorCode.UserAlreadyExists,
                $"Username '{username}' is already taken");
        }

        // Check for existing email
        var existingByEmail = await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        if (existingByEmail != null)
        {
            throw new Application.Exceptions.AuthException(
                AuthErrorCode.UserAlreadyExists,
                $"Email '{email}' is already registered");
        }

        var defaultRoleName = RoleType.MEMBER.ToString();
        // Get the MEMBER role (default role for new registrations)
        var userRole = await _db.UserRoles
            .Where(r => r.Name == defaultRoleName)
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync();

        if (userRole == null)
            throw new Exception($"Default role '{defaultRoleName}' not found. Please ensure roles are seeded.");

        // Hash the password before storing
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            Password = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = _dateTimeProvider.UtcNow,
            RoleId = userRole.Id,
            Role = userRole,
            IsActive = false, // User must verify email before activation
            EmailVerificationToken = null // No longer using token, using OTP instead
        };

        _db.Users.Add(user);
        await _uow.SaveChangesAsync();

        return user;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<Oauthaccount?> GetOAuthAccountAsync(string provider, string providerAccountId)
    {
        return await _db.Oauthaccounts
            .Include(o => o.User)
            .ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(o => o.Provider == provider && o.ProviderAccountId == providerAccountId);
    }

    public async Task AddOAuthAccountAsync(Oauthaccount oauthAccount)
    {
        _db.Oauthaccounts.Add(oauthAccount);
        await _uow.SaveChangesAsync();
    }

    public async Task<User?> VerifyEmailAsync(string email, string otpCode)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otpCode))
            return null;

        // Find user by email
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        // Return null if user not found or already active
        if (user == null || user.IsActive)
            return null;

        // Note: OTP verification is done in the handler before calling this method
        // This method just activates the user account

        // Use domain method instead of directly setting properties
        user.VerifyEmail(_dateTimeProvider);

        await _uow.SaveChangesAsync();
        return user;
    }
}

