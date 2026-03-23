using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Themes.Commands.UpdateTheme
{
    public class UpdateThemeCommand : IRequest<ThemeDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;

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
        public string? ConfigJson { get; set; }
    }
}
