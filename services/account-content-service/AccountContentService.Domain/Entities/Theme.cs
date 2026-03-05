namespace AccountContentService.Domain.Entities;

public class Theme
{
    public Guid Id { get; set; }

    public string ThemeName { get; set; } = string.Empty;

    public string? PrimaryColor { get; set; }

    public string? SecondaryColor { get; set; }

    public string? FontFamily { get; set; }

    public string? CustomCss { get; set; }

    public bool IsActive { get; set; }
}