namespace AuthService.Application.Results;

/// <summary>
/// Result for authentication operations (Login, Register, Google Login, etc.)
/// Contains user info + auth tokens (tokens can be null for unverified users)
/// </summary>
public sealed class AuthResult
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public Guid? RoleId { get; init; }
    public string? RoleName { get; init; }
    public bool IsActive { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? CreatedAt { get; init; }

    /// <summary>
    /// Role-based redirect URL — used by FE to navigate after successful login
    /// instead of inferring from RoleName.
    /// </summary>
    public string? RedirectUrl { get; init; }
}
