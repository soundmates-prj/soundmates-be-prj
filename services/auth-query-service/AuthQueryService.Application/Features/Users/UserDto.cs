namespace AuthQueryService.Application.DTOs
{
    /// <summary>
    /// User DTO for API responses — all fields needed by admin dashboard.
    /// Note: In Query Service, DTOs are populated from Read Models, not Domain Entities.
    /// Canonical status field: AccountStatus (1=Active, 2=Deactivated, 3=Suspended, 4=PendingDeletion).
    /// </summary>
    public sealed class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Guid? RoleId { get; set; }
        public string? RoleName { get; set; }
        public string? Token { get; set; }

        // Canonical account status (mirrors auth-service AccountStatusEnum)
        public int AccountStatus { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public bool IsVerified { get; set; } = false;
        public DateTime? EmailVerifiedAt { get; set; }

        // Ban / suspension fields
        public bool IsBanned { get; set; } = false;
        public DateTime? BannedAt { get; set; }
        public string? BanReason { get; set; }

        // Deactivation fields
        public DateTime? DeactivatedAt { get; set; }
        public string? DeactivationReason { get; set; }

        // Deletion fields
        public DateTime? DeletionRequestedAt { get; set; }
        public DateTime? DeletionScheduledAt { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
