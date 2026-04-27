using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Persistence.Configurations
{
    public class ThemeConfiguration : IEntityTypeConfiguration<Theme>
    {
        public void Configure(EntityTypeBuilder<Theme> builder)
        {
            builder.ToTable("Themes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(150)
                .IsRequired();

            builder.HasIndex(x => x.Name)
                .IsUnique();

            builder.Property(x => x.Mode)
                .HasColumnName("mode")
                .HasMaxLength(20);

            builder.Property(x => x.IsActive)
                .HasColumnName("is_active");

            // Core
            builder.Property(x => x.PrimaryColor)
                .HasColumnName("primary_color")
                .HasMaxLength(20);

            builder.Property(x => x.SecondaryColor)
                .HasColumnName("secondary_color")
                .HasMaxLength(20);

            builder.Property(x => x.BackgroundColor)
                .HasColumnName("background_color")
                .HasMaxLength(20);

            builder.Property(x => x.TextColor)
                .HasColumnName("text_color")
                .HasMaxLength(20);

            // Emotion
            builder.Property(x => x.Mood)
                .HasColumnName("mood")
                .HasMaxLength(50);

            builder.Property(x => x.GradientBackground)
                .HasColumnName("gradient_background")
                .HasColumnType("text");

            // Player
            builder.Property(x => x.PlayerColor)
                .HasColumnName("player_color")
                .HasMaxLength(20);

            // Typography
            builder.Property(x => x.FontFamily)
                .HasColumnName("font_family")
                .HasMaxLength(100);

            // JSON
            builder.Property(x => x.ConfigJson)
                .HasColumnName("config_json")
                .HasColumnType("jsonb");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at");

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");
        }
    }
}
