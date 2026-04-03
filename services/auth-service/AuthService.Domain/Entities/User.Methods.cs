using AuthService.Domain.Interfaces;
using AuthService.Domain.Errors;
using AuthService.Domain.Exceptions;
using AuthService.Domain.ValueObjects;

namespace AuthService.Domain.Entities;

/// <summary>
/// Domain behavior methods for User entity
/// This partial class contains Rich Domain Model methods following Clean Architecture
/// 
/// See User.StateDocumentation.cs for detailed explanation of state transitions
/// </summary>
public partial class User
{
    /// <summary>
    /// Factory method to create a new user (Admin-created users)
    /// 
    /// USAGE: When admin creates a user through admin panel
    /// STATE: Creates user in ACTIVE state (no email verification needed)
    /// 
    /// Example:
    /// <code>
    /// var user = User.CreateAdminUser(
    ///     "johndoe", 
    ///     "john@example.com", 
    ///     hashedPassword, 
    ///     "John", 
    ///     "Doe", 
    ///     roleId, 
    ///     dateTimeProvider);
    /// </code>
    /// </summary>
    /// <exception cref="UserValidationException">When validation fails</exception>
    public static User CreateAdminUser(
        string username,
        string email,
        string hashedPassword,
        string? firstName,
        string? lastName,
        Guid? roleId,
        IDateTimeProvider dateTimeProvider)
    {
        // Validate username
        if (string.IsNullOrWhiteSpace(username))
            throw new UserValidationException(
                "Username cannot be empty", 
                UserErrorCodes.UsernameEmpty);

        // Validate email using Value Object (better validation)
        var validatedEmail = ValueObjects.Email.Create(email);

        return new User
        {
            Id = Guid.NewGuid(),
            Username = username.Trim(),
            Email = validatedEmail.Value, // Already normalized
            Password = hashedPassword,
            FirstName = firstName?.Trim(),
            LastName = lastName?.Trim(),
            RoleId = roleId,
            IsActive = true, // Admin-created users are active by default
            CreatedAt = dateTimeProvider.UtcNow,
            UpdatedAt = null
        };
    }

    /// <summary>
    /// Updates user profile information with validation
    /// 
    /// USAGE: Admin updates user information through admin panel
    /// STATE: User must be in any state (works for active/inactive)
    /// SIDE EFFECTS: Sets UpdatedAt timestamp
    /// 
    /// Example:
    /// <code>
    /// user.UpdateProfile("newusername", "newemail@example.com", "John", "Doe", roleId, dateTimeProvider);
    /// </code>
    /// </summary>
    /// <exception cref="UserValidationException">When validation fails</exception>
    public void UpdateProfile(
        string username,
        string email,
        string? firstName,
        string? lastName,
        Guid? roleId,
        IDateTimeProvider dateTimeProvider)
    {
        // Validate username
        if (string.IsNullOrWhiteSpace(username))
            throw new UserValidationException(
                "Username cannot be empty", 
                UserErrorCodes.UsernameEmpty);

        // Validate email using Value Object
        var validatedEmail = ValueObjects.Email.Create(email);

        // Update properties (encapsulated logic)
        Username = username.Trim();
        Email = validatedEmail.Value;
        FirstName = firstName?.Trim();
        LastName = lastName?.Trim();
        RoleId = roleId;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Updates only first name and last name (for profile update)
    /// 
    /// USAGE: User updates their own name through profile page
    /// STATE: User should be ACTIVE
    /// SIDE EFFECTS: Sets UpdatedAt timestamp
    /// 
    /// Example:
    /// <code>
    /// user.UpdateName("John", "Smith", dateTimeProvider);
    /// </code>
    /// </summary>
    /// <exception cref="UserValidationException">When validation fails</exception>
    public void UpdateName(
        string firstName,
        string lastName,
        IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new UserValidationException(
                "First name cannot be empty", 
                UserErrorCodes.FirstNameEmpty);

        if (string.IsNullOrWhiteSpace(lastName))
            throw new UserValidationException(
                "Last name cannot be empty", 
                UserErrorCodes.LastNameEmpty);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Activates the user account
    /// 
    /// USAGE: Admin activates a deactivated or banned user
    /// STATE TRANSITION: INACTIVE/DEACTIVATED/BANNED ? ACTIVE
    /// SIDE EFFECTS: Sets IsActive=true, UpdatedAt timestamp
    /// 
    /// Example:
    /// <code>
    /// user.Activate(dateTimeProvider); // User can now login
    /// </code>
    /// </summary>
    /// <exception cref="InvalidUserStateException">When user is already active</exception>
    public void Activate(IDateTimeProvider dateTimeProvider)
    {
        if (IsActive)
            throw new InvalidUserStateException(
                "User is already active", 
                UserErrorCodes.AlreadyActive);

        IsActive = true;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Deactivates the user account (soft delete)
    /// 
    /// USAGE: User chooses to deactivate their own account OR admin deactivates
    /// STATE TRANSITION: ACTIVE ? DEACTIVATED
    /// SIDE EFFECTS: Sets IsActive=false, UpdatedAt timestamp
    /// IMPORTANT: This is different from Ban() - Deactivate is voluntary, Ban is punishment
    /// 
    /// Example:
    /// <code>
    /// user.Deactivate(dateTimeProvider); // User cannot login anymore
    /// </code>
    /// </summary>
    /// <exception cref="InvalidUserStateException">When user is already inactive</exception>
    public void Deactivate(IDateTimeProvider dateTimeProvider)
    {
        if (!IsActive)
            throw new InvalidUserStateException(
                "User is already inactive", 
                UserErrorCodes.AlreadyInactive);

        IsActive = false;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Bans the user account
    /// 
    /// USAGE: Admin bans user for policy violation or abuse
    /// STATE TRANSITION: ACTIVE ? BANNED
    /// SIDE EFFECTS: Sets IsActive=false, UpdatedAt timestamp
    /// IMPORTANT: Cannot ban already inactive user (must be active first)
    /// 
    /// Example:
    /// <code>
    /// user.Ban(dateTimeProvider); // User is banned and cannot login
    /// </code>
    /// </summary>
    /// <exception cref="InvalidUserStateException">When user is already inactive</exception>
    public void Ban(IDateTimeProvider dateTimeProvider)
    {
        if (!IsActive)
            throw new InvalidUserStateException(
                "Cannot ban an already inactive user", 
                UserErrorCodes.CannotBanInactive);

        IsActive = false;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Unbans the user account
    /// 
    /// USAGE: Admin unbans a previously banned user
    /// STATE TRANSITION: BANNED ? ACTIVE
    /// SIDE EFFECTS: Sets IsActive=true, UpdatedAt timestamp
    /// 
    /// Example:
    /// <code>
    /// user.Unban(dateTimeProvider); // User can login again
    /// </code>
    /// </summary>
    /// <exception cref="InvalidUserStateException">When user is already active</exception>
    public void Unban(IDateTimeProvider dateTimeProvider)
    {
        if (IsActive)
            throw new InvalidUserStateException(
                "User is already active", 
                UserErrorCodes.AlreadyActive);

        IsActive = true;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Verifies user email
    /// 
    /// USAGE: User enters correct OTP code to verify their email
    /// STATE TRANSITION: INACTIVE (just registered) ? ACTIVE
    /// SIDE EFFECTS: 
    ///   - Sets EmailVerifiedAt timestamp
    ///   - Clears EmailVerificationToken
    ///   - Sets IsActive=true (user can now login)
    ///   - Sets UpdatedAt timestamp
    /// 
    /// Example:
    /// <code>
    /// user.VerifyEmail(dateTimeProvider); // User is now verified and can login
    /// </code>
    /// </summary>
    /// <exception cref="InvalidUserStateException">When email is already verified</exception>
    public void VerifyEmail(IDateTimeProvider dateTimeProvider)
    {
        if (EmailVerifiedAt.HasValue)
            throw new InvalidUserStateException(
                "Email is already verified", 
                UserErrorCodes.EmailAlreadyVerified);

        EmailVerifiedAt = dateTimeProvider.UtcNow;
        EmailVerificationToken = null;
        IsActive = true; // Activate user upon email verification
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Changes user password
    /// 
    /// USAGE: User changes their password (must provide old password first - validated in handler)
    /// STATE: User should be ACTIVE
    /// SIDE EFFECTS: Sets Password (hashed), UpdatedAt timestamp
    /// SECURITY: Password should already be hashed before calling this method
    /// 
    /// Example:
    /// <code>
    /// var hashedPassword = BCrypt.HashPassword(newPassword);
    /// user.ChangePassword(hashedPassword, dateTimeProvider);
    /// </code>
    /// </summary>
    /// <exception cref="UserValidationException">When password is empty</exception>
    public void ChangePassword(string hashedPassword, IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new UserValidationException(
                "Password cannot be empty", 
                UserErrorCodes.PasswordEmpty);

        Password = hashedPassword;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Sets email verification token (legacy - now using OTP)
    ///
    /// USAGE: Generate token for email verification link (old method)
    /// STATE: User just registered (INACTIVE)
    /// SIDE EFFECTS: Sets EmailVerificationToken, UpdatedAt timestamp
    /// NOTE: This method is kept for backward compatibility, but OTP is now preferred
    ///
    /// Example:
    /// <code>
    /// var token = Guid.NewGuid().ToString();
    /// user.SetEmailVerificationToken(token, dateTimeProvider);
    /// </code>
    /// </summary>
    /// <exception cref="UserValidationException">When token is empty</exception>
    public void SetEmailVerificationToken(string token, IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new UserValidationException(
                "Token cannot be empty",
                UserErrorCodes.TokenEmpty);

        EmailVerificationToken = token;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ACCOUNT DEACTIVATION & DELETION
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Member-initiated account deactivation (soft lock — reversible within 90 days).
    /// Sets IsActive = false so the user cannot login, but data is preserved.
    /// Reason is stored for analytics.
    ///
    /// USAGE: Member goes to Settings → Tài khoản → Vô hiệu hóa
    /// STATE TRANSITION: ACTIVE → DEACTIVATED
    /// SIDE EFFECTS: Sets IsActive = false, DeactivationReason, UpdatedAt
    ///
    /// Example:
    /// <code>
    /// user.DeactivateWithReason("Tạm nghỉ", dateTimeProvider);
    /// </code>
    /// </summary>
    /// <exception cref="InvalidUserStateException">When user is not active</exception>
    public void DeactivateWithReason(string reason, IDateTimeProvider dateTimeProvider)
    {
        if (!IsActive)
            throw new InvalidUserStateException(
                "Account is already inactive",
                UserErrorCodes.AlreadyInactive);

        IsActive = false;
        DeactivationReason = reason;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Marks the account for permanent deletion (30-day grace period).
    /// User CAN still log in during grace period to cancel this.
    /// After DeletionScheduledAt passes, a background job permanently deletes data.
    ///
    /// USAGE: Member goes to Settings → Tài khoản → Xóa vĩnh viễn
    /// STATE TRANSITION: ACTIVE → PENDING_DELETION
    /// SIDE EFFECTS: Sets IsActive = false, DeletionRequestedAt, DeletionScheduledAt (+30 days)
    ///
    /// Example:
    /// <code>
    /// user.MarkForDeletion(dateTimeProvider);
    /// </code>
    /// </summary>
    /// <exception cref="InvalidUserStateException">When user is not active</exception>
    public void MarkForDeletion(IDateTimeProvider dateTimeProvider)
    {
        if (!IsActive)
            throw new InvalidUserStateException(
                "Cannot mark inactive account for deletion",
                UserErrorCodes.AlreadyInactive);

        IsActive = false;
        DeletionRequestedAt = dateTimeProvider.UtcNow;
        DeletionScheduledAt = dateTimeProvider.UtcNow.AddDays(30);
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Cancels a pending permanent deletion request.
    /// Restores account to active state and clears deletion metadata.
    ///
    /// USAGE: Member logs in during 30-day grace period and cancels deletion.
    /// STATE TRANSITION: PENDING_DELETION → ACTIVE
    /// SIDE EFFECTS: Sets IsActive = true, clears DeletionRequestedAt & DeletionScheduledAt
    ///
    /// Example:
    /// <code>
    /// user.CancelDeletionRequest(dateTimeProvider);
    /// </code>
    /// </summary>
    /// <exception cref="InvalidUserStateException">When no deletion is pending</exception>
    public void CancelDeletionRequest(IDateTimeProvider dateTimeProvider)
    {
        if (!DeletionScheduledAt.HasValue)
            throw new InvalidUserStateException(
                "No deletion request to cancel",
                UserErrorCodes.AlreadyActive);

        IsActive = true;
        DeletionRequestedAt = null;
        DeletionScheduledAt = null;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Returns the current account status as a human-readable string.
    /// Used by the login handler and profile endpoints.
    /// </summary>
    public string GetAccountStatusText()
    {
        if (DeletionScheduledAt.HasValue && DeletionScheduledAt > DateTime.UtcNow)
            return "PendingDeletion";
        if (!IsActive)
            return "Deactivated";
        return "Active";
    }
}
