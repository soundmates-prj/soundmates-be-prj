using System.Text.RegularExpressions;
using LiveSessionService.Domain.Errors;
using LiveSessionService.Domain.Exceptions;

namespace LiveSessionService.Domain.ValueObjects;

/// <summary>
/// Value Object representing a validated stream URL
/// Ensures URL is always in valid format (immutable)
/// </summary>
public sealed record StreamUrl
{
    private static readonly Regex UrlRegex = new(
        @"^https?:\/\/.+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private StreamUrl(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new StreamUrl value object with validation
    /// </summary>
    /// <param name="url">URL string to validate</param>
    /// <returns>Valid StreamUrl object</returns>
    /// <exception cref="AzuraCastStationValidationException">When URL is invalid</exception>
    public static StreamUrl Create(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new AzuraCastStationValidationException(
                "Stream URL cannot be empty",
                AzuraCastStationErrorCodes.StreamUrlEmpty);

        var trimmedUrl = url.Trim();

        if (!UrlRegex.IsMatch(trimmedUrl))
            throw new AzuraCastStationValidationException(
                "Invalid stream URL format. Must start with http:// or https://",
                AzuraCastStationErrorCodes.StreamUrlInvalid);

        return new StreamUrl(trimmedUrl);
    }

    /// <summary>
    /// Implicit conversion from StreamUrl to string for convenience
    /// </summary>
    public static implicit operator string(StreamUrl url) => url.Value;

    public override string ToString() => Value;
}
