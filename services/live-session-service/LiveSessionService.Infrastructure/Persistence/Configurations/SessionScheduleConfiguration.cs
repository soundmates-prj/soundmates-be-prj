using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public sealed class SessionScheduleConfiguration : IEntityTypeConfiguration<SessionSchedule>
{
    public void Configure(EntityTypeBuilder<SessionSchedule> builder)
    {
        builder.ToTable("session_schedules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.StartTime)
            .HasColumnName("start_time")
            .HasColumnType("time")
            .IsRequired();

        builder.Property(x => x.EndTime)
            .HasColumnName("end_time")
            .HasColumnType("time")
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(300);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(ScheduleStatus.Scheduled);

        builder.Property(x => x.IsRecurring)
            .HasColumnName("is_recurring")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DaysOfWeek)
            .HasColumnName("days_of_week")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(DaysOfWeek.None);

        builder.Property(x => x.StartDate)
            .HasColumnName("start_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.EndDate)
            .HasColumnName("end_date")
            .HasColumnType("date");

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(x => x.LiveSessionId)
            .HasColumnName("live_session_id")
            .IsRequired();

        builder.HasOne(x => x.LiveSession)
            .WithMany(x => x.SessionSchedules)
            .HasForeignKey(x => x.LiveSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LiveSessionId);
        builder.HasIndex(x => x.StartDate);
    }
}
