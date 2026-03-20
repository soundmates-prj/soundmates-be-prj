using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.DTOs
{
    public class ThemeDto
    {
        public Guid Id { get; set; }

        public string ThemeName { get; set; }

        public string? PrimaryColor { get; set; }

        public string? SecondaryColor { get; set; }

        public string? FontFamily { get; set; }

        public string? CustomCss { get; set; }

        public bool IsActive { get; set; }
    }
}
