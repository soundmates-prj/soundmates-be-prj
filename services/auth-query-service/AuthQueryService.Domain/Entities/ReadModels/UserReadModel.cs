namespace AuthQueryService.Domain.Entities.ReadModels
{
    /// <summary>
    /// Enum values must stay in sync with AuthService.Application.Enums.AccountStatusEnum
    /// 1 = Active, 2 = Deactivated, 3 = Suspended, 4 = PendingDeletion
    /// </summary>
    public sealed class UserReadModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Guid? RoleId { get; set; }
        public string? RoleName { get; set; }
        public bool IsActive { get; set; }
        /// <summary>
        /// Canonical account status matching auth-service.
        /// 1 = Active, 2 = Deactivated, 3 = Suspended, 4 = PendingDeletion
        /// </summary>
        public int AccountStatus { get; set; } = 1;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Email verification
        public bool IsVerified { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }

        // Ban fields
        public bool IsBanned { get; set; }
        public DateTime? BannedAt { get; set; }
        public string? BanReason { get; set; }

        // Deactivation
        public DateTime? DeactivatedAt { get; set; }
        public string? DeactivationReason { get; set; }

        // Permanent deletion
        public DateTime? DeletionRequestedAt { get; set; }
        public DateTime? DeletionScheduledAt { get; set; }

        // Profile fields
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? BackgroundImageUrl { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Location { get; set; }
        public string? Website { get; set; }
    }
}