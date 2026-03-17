namespace AuthService.Domain.Errors;

public static class UserFavouriteErrorCodes
{
    public const string UserIdEmpty = "FAVOURITE_USER_ID_EMPTY";
    public const string ItemTypeEmpty = "FAVOURITE_ITEM_TYPE_EMPTY";
    public const string ItemTypeInvalid = "FAVOURITE_ITEM_TYPE_INVALID";
    public const string ItemIdEmpty = "FAVOURITE_ITEM_ID_EMPTY";
    public const string ItemIdTooLong = "FAVOURITE_ITEM_ID_TOO_LONG";
    public const string SourceEmpty = "FAVOURITE_SOURCE_EMPTY";
    public const string SourceInvalid = "FAVOURITE_SOURCE_INVALID";
}
