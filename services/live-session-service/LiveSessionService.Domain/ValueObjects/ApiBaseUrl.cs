using System.Text.RegularExpressions;
using LiveSessionService.Domain.Errors;
using LiveSessionService.Domain.Exceptions;

namespace LiveSessionService.Domain.ValueObjects;

/// <summary>
/// Value Object representing a validated API base URL
/// Ensures URL is always in valid format (immutable)
/// </summary>
public sealed record ApiBaseUrl
{
    private static readonly Regex UrlRegex = new(
        @"^https?:\/\/[a-zA-Z0-9\-\.]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private ApiBaseUrl(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new ApiBaseUrl value object with validation
    /// </summary>
    /// <param name="url">URL string to validate</param>
    /// <returns>Valid ApiBaseUrl object</returns>
    /// <exception cref="AzuraCastStationValidationException">When URL is invalid</exception>
    public static ApiBaseUrl Create(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new AzuraCastStationValidationException(
                "API base URL cannot be empty",
                AzuraCastStationErrorCodes.ApiBaseUrlEmpty);

        var trimmedUrl = url.Trim().TrimEnd('/'); // Remove trailing slash

        if (!UrlRegex.IsMatch(trimmedUrl))
            throw new AzuraCastStationValidationException(
                "Invalid API base URL format",
                AzuraCastStationErrorCodes.ApiBaseUrlInvalid);

        return new ApiBaseUrl(trimmedUrl);
    }

    /// <summary>
    /// Implicit conversion from ApiBaseUrl to string
    /// </summary>
    public static implicit operator string(ApiBaseUrl url) => url.Value;

    public override string ToString() => Value;
}
