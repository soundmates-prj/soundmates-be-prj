using AuthService.Domain.Errors;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;

namespace AuthService.Domain.Entities;

public partial class UserFavourite
{
    public static UserFavourite Create(
        Guid userId,
        string itemType,
        string itemId,
        string source,
        IDateTimeProvider dateTimeProvider)
    {
        if (userId == Guid.Empty)
            throw new UserFavouriteValidationException("UserId is required", UserFavouriteErrorCodes.UserIdEmpty);

        var normalizedItemType = NormalizeItemType(itemType);
        var normalizedItemId = NormalizeItemId(itemId);
        var normalizedSource = NormalizeSource(source);

        return new UserFavourite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemType = normalizedItemType,
            ItemId = normalizedItemId,
            Source = normalizedSource,
            CreatedAt = dateTimeProvider.UtcNow,
            UpdatedAt = dateTimeProvider.UtcNow
        };
    }

    public static string NormalizeItemType(string itemType)
    {
        if (string.IsNullOrWhiteSpace(itemType))
            throw new UserFavouriteValidationException("itemType is required", UserFavouriteErrorCodes.ItemTypeEmpty);

        var normalized = itemType.Trim();
        if (!Enum.TryParse<FavouriteItemType>(normalized, true, out var parsed))
            throw new UserFavouriteValidationException(
                $"itemType '{normalized}' is not supported",
                UserFavouriteErrorCodes.ItemTypeInvalid);

        return parsed.ToString().ToLowerInvariant();
    }

    public static string NormalizeItemId(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new UserFavouriteValidationException("itemId is required", UserFavouriteErrorCodes.ItemIdEmpty);

        var normalized = itemId.Trim();
        if (normalized.Length > 200)
            throw new UserFavouriteValidationException("itemId length cannot exceed 200", UserFavouriteErrorCodes.ItemIdTooLong);

        return normalized;
    }

    public static string NormalizeSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new UserFavouriteValidationException("source is required", UserFavouriteErrorCodes.SourceEmpty);

        var normalized = source.Trim();
        if (!Enum.TryParse<FavouriteSource>(normalized, true, out var parsed))
            throw new UserFavouriteValidationException(
                $"source '{normalized}' is not supported",
                UserFavouriteErrorCodes.SourceInvalid);

        return parsed.ToString().ToLowerInvariant();
    }
}
