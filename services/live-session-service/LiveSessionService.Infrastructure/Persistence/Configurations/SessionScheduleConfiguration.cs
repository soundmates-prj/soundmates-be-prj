using LiveSessionService.Domain.Entities;
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
            .IsRequired();

        builder.Property(x => x.EndTime)
            .HasColumnName("end_time")
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(50);

        builder.Property(x => x.LiveSessionId)
            .HasColumnName("live_session_id")
            .IsRequired();

        builder.HasOne(x => x.LiveSession)
            .WithMany(x => x.SessionSchedules)
            .HasForeignKey(x => x.LiveSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LiveSessionId);
        builder.HasIndex(x => x.StartTime);
    }
}
