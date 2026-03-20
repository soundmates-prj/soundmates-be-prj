using AuthService.Domain.Errors;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;

namespace AuthService.Domain.Entities;

public partial class SpotifyItem
{
    public static SpotifyItem Create(
        string spotifyId,
        string itemType,
        string? name,
        string? artistName,
        string? albumName,
        string? imgUrl,
        string? previewUrl,
        string? rawJson,
        IDateTimeProvider dateTimeProvider)
    {
        var normalizedSpotifyId = NormalizeSpotifyId(spotifyId);
        var normalizedItemType = NormalizeItemType(itemType);

        return new SpotifyItem
        {
            Id = Guid.NewGuid(),
            SpotifyId = normalizedSpotifyId,
            ItemType = normalizedItemType,
            Name = ValidateMaxLength(name?.Trim() ?? string.Empty, 255, SpotifyItemErrorCodes.NameTooLong, "name"),
            ArtistName = ValidateMaxLength(artistName?.Trim() ?? string.Empty, 255, SpotifyItemErrorCodes.ArtistNameTooLong, "artistName"),
            AlbumName = ValidateMaxLength(albumName?.Trim() ?? string.Empty, 255, SpotifyItemErrorCodes.AlbumNameTooLong, "albumName"),
            ImgUrl = ValidateMaxLength(imgUrl?.Trim() ?? string.Empty, 500, SpotifyItemErrorCodes.ImgUrlTooLong, "imgUrl"),
            PreviewUrl = ValidateMaxLength(previewUrl?.Trim() ?? string.Empty, 500, SpotifyItemErrorCodes.PreviewUrlTooLong, "previewUrl"),
            RawJson = rawJson?.Trim() ?? string.Empty,
            UpdatedAt = dateTimeProvider.UtcNow
        };
    }

    public void UpdateMetadata(
        string? name,
        string? artistName,
        string? albumName,
        string? imgUrl,
        string? previewUrl,
        string? rawJson,
        IDateTimeProvider dateTimeProvider)
    {
        Name = ValidateMaxLength(name?.Trim() ?? string.Empty, 255, SpotifyItemErrorCodes.NameTooLong, "name");
        ArtistName = ValidateMaxLength(artistName?.Trim() ?? string.Empty, 255, SpotifyItemErrorCodes.ArtistNameTooLong, "artistName");
        AlbumName = ValidateMaxLength(albumName?.Trim() ?? string.Empty, 255, SpotifyItemErrorCodes.AlbumNameTooLong, "albumName");
        ImgUrl = ValidateMaxLength(imgUrl?.Trim() ?? string.Empty, 500, SpotifyItemErrorCodes.ImgUrlTooLong, "imgUrl");
        PreviewUrl = ValidateMaxLength(previewUrl?.Trim() ?? string.Empty, 500, SpotifyItemErrorCodes.PreviewUrlTooLong, "previewUrl");
        RawJson = rawJson?.Trim() ?? string.Empty;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    public static string NormalizeSpotifyId(string spotifyId)
    {
        if (string.IsNullOrWhiteSpace(spotifyId))
            throw new SpotifyItemValidationException("spotifyId is required", SpotifyItemErrorCodes.SpotifyIdEmpty);

        var normalized = spotifyId.Trim();
        if (normalized.Length > 200)
            throw new SpotifyItemValidationException("spotifyId length cannot exceed 200", SpotifyItemErrorCodes.SpotifyIdTooLong);

        return normalized;
    }

    public static string NormalizeItemType(string itemType)
    {
        if (string.IsNullOrWhiteSpace(itemType))
            throw new SpotifyItemValidationException("itemType is required", SpotifyItemErrorCodes.ItemTypeEmpty);

        var normalized = itemType.Trim();
        if (!Enum.TryParse<SpotifyItemType>(normalized, true, out var parsed))
            throw new SpotifyItemValidationException(
                $"itemType '{normalized}' is not supported for Spotify item",
                SpotifyItemErrorCodes.ItemTypeInvalid);

        return parsed.ToString().ToLowerInvariant();
    }

    private static string ValidateMaxLength(string value, int maxLength, string errorCode, string fieldName)
    {
        if (value.Length > maxLength)
            throw new SpotifyItemValidationException($"{fieldName} length cannot exceed {maxLength}", errorCode);

        return value;
    }
}
