namespace AuthQueryService.Application.DTOs
{
    /// <summary>
    /// User read DTO returned by all user query endpoints.
    /// Canonical status: AccountStatus (1=Active, 2=Deactivated, 3=Suspended, 4=PendingDeletion).
    /// </summary>
    public sealed class UserReadDto
    {
        public Guid Id { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public Guid? RoleId { get; init; }
        public string? RoleName { get; init; }
        public string? AvatarUrl { get; init; }

        // Canonical account status
        public int AccountStatus { get; init; } = 1;
        public bool IsActive { get; init; }
        public bool IsVerified { get; init; }
        public DateTime? EmailVerifiedAt { get; init; }

        // Ban / suspension
        public bool IsBanned { get; init; }
        public DateTime? BannedAt { get; init; }
        public string? BanReason { get; init; }

        // Deactivation
        public DateTime? DeactivatedAt { get; init; }
        public string? DeactivationReason { get; init; }

        // Deletion
        public DateTime? DeletionRequestedAt { get; init; }
        public DateTime? DeletionScheduledAt { get; init; }

        public DateTime? CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}