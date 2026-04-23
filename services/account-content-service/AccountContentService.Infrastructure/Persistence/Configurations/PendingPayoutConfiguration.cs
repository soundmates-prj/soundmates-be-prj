using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountContentService.Infrastructure.Persistence.Configurations;

public class PendingPayoutConfiguration : IEntityTypeConfiguration<PendingPayout>
{
    public void Configure(EntityTypeBuilder<PendingPayout> builder)
    {
        builder.ToTable("pending_payouts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.BankId)
            .HasMaxLength(50);

        builder.Property(x => x.AccountNumber)
            .HasMaxLength(50);

        builder.Property(x => x.AccountName)
            .HasMaxLength(255);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ScheduledAt);
    }
}
