namespace AccountContentService.Api.Contracts.Requests
{
    public class ThemeRequest
    {
        public required string ThemeName { get; set; }

        public required string PrimaryColor { get; set; }

        public required string SecondaryColor { get; set; }

        public required string FontFamily { get; set; }

        public string? CustomCss { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
