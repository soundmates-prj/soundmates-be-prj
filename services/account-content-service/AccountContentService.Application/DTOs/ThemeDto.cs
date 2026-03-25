using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AccountContentService.Application.DTOs
{
    public class ThemeDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!; // unique

        public string Mode { get; set; } = "light";
        public bool IsActive { get; set; } = true;

        // Core
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? BackgroundColor { get; set; }
        public string? TextColor { get; set; }

        // Emotion
        public string? Mood { get; set; }
        public string? GradientBackground { get; set; }

        // Player
        public string? PlayerColor { get; set; }

        // Typography
        public string? FontFamily { get; set; }

        // Advanced (JSON config)
        public JsonElement? ConfigJson { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
