using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountContentService.Infrastructure.Persistence.Configurations;

internal class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlanName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Price)
            .HasPrecision(18, 2);
    }
}

internal class UserVoiceModelConfiguration : IEntityTypeConfiguration<UserVoiceModel>
{
    public void Configure(EntityTypeBuilder<UserVoiceModel> builder)
    {
        builder.ToTable("UserVoiceModels");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DisplayName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.VoiceCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.SourceAudioUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .IsRequired();

        // Index for fast lookup by user
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
    }
}
