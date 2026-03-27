using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountContentService.Infrastructure.Persistence.Configurations;

internal sealed class UserProfileReadModelConfiguration
    : IEntityTypeConfiguration<UserProfileReadModel>
{
    public void Configure(EntityTypeBuilder<UserProfileReadModel> builder)
    {
        builder.ToTable("user_profile_read_models");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(128);

        builder.Property(x => x.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(128);

        builder.Property(x => x.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(1024);

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(256);

        builder.Property(x => x.IsPending)
            .HasColumnName("is_pending")
            .HasDefaultValue(false);

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        // Fast lookup by Id
        builder.HasIndex(x => x.Id);
    }
}
