using LiveSessionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public class SyncAuditLogConfiguration : IEntityTypeConfiguration<SyncAuditLog>
{
    public void Configure(EntityTypeBuilder<SyncAuditLog> builder)
    {
        builder.ToTable("sync_audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.SyncType)
            .HasColumnName("sync_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.StationId)
            .HasColumnName("station_id");

        builder.Property(x => x.TriggeredByUserId)
            .HasColumnName("triggered_by_user_id")
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.TotalRecords)
            .HasColumnName("total_records")
            .IsRequired();

        builder.Property(x => x.CreatedRecords)
            .HasColumnName("created_records")
            .IsRequired();

        builder.Property(x => x.UpdatedRecords)
            .HasColumnName("updated_records")
            .IsRequired();

        builder.Property(x => x.DeletedRecords)
            .HasColumnName("deleted_records")
            .IsRequired();

        builder.Property(x => x.FailedRecords)
            .HasColumnName("failed_records")
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(2000);

        builder.Property(x => x.ErrorDetails)
            .HasColumnName("error_details")
            .HasColumnType("jsonb");

        builder.Property(x => x.DurationSeconds)
            .HasColumnName("duration_seconds")
            .IsRequired();

        // Indexes for querying
        builder.HasIndex(x => x.SyncType)
            .HasDatabaseName("ix_sync_audit_logs_sync_type");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_sync_audit_logs_status");

        builder.HasIndex(x => x.StartedAt)
            .HasDatabaseName("ix_sync_audit_logs_started_at");

        builder.HasIndex(x => x.TriggeredByUserId)
            .HasDatabaseName("ix_sync_audit_logs_triggered_by_user_id");

        builder.HasIndex(x => x.StationId)
            .HasDatabaseName("ix_sync_audit_logs_station_id");
    }
}
