using System.Text.RegularExpressions;

namespace LiveSessionService.Application.Services;

/// <summary>
/// Service for validating and sanitizing input data from external sources (AzuraCast)
/// </summary>
public interface IInputValidationService
{
    string SanitizeString(string? input, int maxLength, string defaultValue = "");
    string SanitizeTitle(string? input);
    string SanitizeArtist(string? input);
    string SanitizeAlbum(string? input);
    string SanitizeGenre(string? input);
    string SanitizeFilePath(string? input);
    string? SanitizeUrl(string? input);
    int ValidateDuration(double duration);
    long ValidateFileSize(long fileSize);
}

public sealed class InputValidationService : IInputValidationService
{
    private const int MaxTitleLength = 300;
    private const int MaxArtistLength = 200;
    private const int MaxAlbumLength = 200;
    private const int MaxGenreLength = 100;
    private const int MaxFilePathLength = 1000;
    private const int MaxUrlLength = 2000;
    private const int MaxDurationSeconds = 86400; // 24 hours
    private const long MaxFileSize = 1073741824; // 1GB

    // Regex patterns for sanitization
    private static readonly Regex DangerousCharsRegex = new(@"[<>""'%;()&+]", RegexOptions.Compiled);
    private static readonly Regex PathTraversalRegex = new(@"\.\.|[/\\]", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex UrlRegex = new(@"^https?://", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string SanitizeString(string? input, int maxLength, string defaultValue = "")
    {
        if (string.IsNullOrWhiteSpace(input))
            return defaultValue;

        // Remove dangerous characters
        var sanitized = DangerousCharsRegex.Replace(input, "");
        
        // Normalize whitespace
        sanitized = WhitespaceRegex.Replace(sanitized, " ");
        
        // Trim
        sanitized = sanitized.Trim();
        
        // Truncate if too long
        if (sanitized.Length > maxLength)
            sanitized = sanitized.Substring(0, maxLength);

        return string.IsNullOrWhiteSpace(sanitized) ? defaultValue : sanitized;
    }

    public string SanitizeTitle(string? input)
    {
        return SanitizeString(input, MaxTitleLength, "Unknown Title");
    }

    public string SanitizeArtist(string? input)
    {
        return SanitizeString(input, MaxArtistLength, "Unknown Artist");
    }

    public string SanitizeAlbum(string? input)
    {
        return SanitizeString(input, MaxAlbumLength, "");
    }

    public string SanitizeGenre(string? input)
    {
        return SanitizeString(input, MaxGenreLength, "");
    }

    public string SanitizeFilePath(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        // Remove path traversal attempts
        var sanitized = PathTraversalRegex.Replace(input, "");
        
        // Remove dangerous characters
        sanitized = DangerousCharsRegex.Replace(sanitized, "");
        
        // Truncate if too long
        if (sanitized.Length > MaxFilePathLength)
            sanitized = sanitized.Substring(0, MaxFilePathLength);

        return sanitized.Trim();
    }

    public string? SanitizeUrl(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        // Basic URL validation
        if (!UrlRegex.IsMatch(input))
            return null;

        // Truncate if too long
        if (input.Length > MaxUrlLength)
            input = input.Substring(0, MaxUrlLength);

        // Try to parse as URI
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
            return null;

        // Only allow http/https
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return null;

        return uri.ToString();
    }

    public int ValidateDuration(double duration)
    {
        var durationInt = (int)Math.Max(0, duration);
        
        // Cap at max duration
        if (durationInt > MaxDurationSeconds)
            durationInt = MaxDurationSeconds;

        return durationInt;
    }

    public long ValidateFileSize(long fileSize)
    {
        if (fileSize < 0)
            return 0;

        // Cap at max file size
        if (fileSize > MaxFileSize)
            return MaxFileSize;

        return fileSize;
    }
}
