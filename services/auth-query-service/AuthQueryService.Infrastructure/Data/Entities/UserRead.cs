using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthQueryService.Infrastructure.Data.Entities
{
    /// <summary>
    /// Enum values must stay in sync with AuthService.Application.Enums.AccountStatusEnum
    /// 1 = Active, 2 = Deactivated, 3 = Suspended, 4 = PendingDeletion
    /// </summary>
    public sealed class UserRead
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Guid? RoleId { get; set; }
        public string? RoleName { get; set; }
        public bool IsActive { get; set; } = true;
        /// <summary>
        /// Canonical account status matching auth-service.
        /// 1 = Active, 2 = Deactivated, 3 = Suspended, 4 = PendingDeletion
        /// </summary>
        public int AccountStatus { get; set; } = 1;
        public bool IsBanned { get; set; } = false;
        public DateTime? DeactivatedAt { get; set; }
        public string? DeactivationReason { get; set; }
        public DateTime? BannedAt { get; set; }
        public string? BanReason { get; set; }
        public DateTime? DeletionRequestedAt { get; set; }
        public DateTime? DeletionScheduledAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    internal sealed class UserReadConfiguration : IEntityTypeConfiguration<UserRead>
    {
        public void Configure(EntityTypeBuilder<UserRead> b)
        {
            b.ToTable("users_read");
            b.HasKey(x => x.Id);
            b.Property(x => x.Username).HasMaxLength(128).IsRequired();
            b.Property(x => x.Email).HasMaxLength(256).IsRequired();
            b.Property(x => x.FirstName).HasMaxLength(128);
            b.Property(x => x.LastName).HasMaxLength(128);
            b.Property(x => x.RoleName).HasMaxLength(128);
            b.Property(x => x.DeactivationReason).HasMaxLength(512);
            b.Property(x => x.BanReason).HasMaxLength(512);
            b.HasIndex(x => x.Username).IsUnique();
            b.HasIndex(x => x.Email).IsUnique();
        }
    }
}