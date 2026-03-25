using System.Text.Json;

namespace AccountContentService.Api.Contracts.Requests
{
    public class ThemeRequest
    {
        public required string Name { get; set; } = null!;

        public string Mode { get; set; } = "light";

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
    }
}
