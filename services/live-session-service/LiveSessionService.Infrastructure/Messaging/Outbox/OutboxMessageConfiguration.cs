using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.OccurredOnUtc)
            .HasColumnName("occurred_on_utc")
            .IsRequired();

        builder.Property(x => x.ProcessedOnUtc)
            .HasColumnName("processed_on_utc");

        builder.Property(x => x.Error)
            .HasColumnName("error")
            .HasMaxLength(2000);

        builder.Property(x => x.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0)
            .IsRequired();

        // Indexes for outbox processing
        builder.HasIndex(x => x.ProcessedOnUtc);
        builder.HasIndex(x => x.OccurredOnUtc);
        builder.HasIndex(x => new { x.ProcessedOnUtc, x.RetryCount });
    }
}
