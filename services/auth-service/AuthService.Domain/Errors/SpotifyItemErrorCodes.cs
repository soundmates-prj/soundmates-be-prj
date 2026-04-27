namespace AuthService.Domain.Errors;

public static class SpotifyItemErrorCodes
{
    public const string SpotifyIdEmpty = "SPOTIFY_ITEM_ID_EMPTY";
    public const string SpotifyIdTooLong = "SPOTIFY_ITEM_ID_TOO_LONG";
    public const string ItemTypeEmpty = "SPOTIFY_ITEM_TYPE_EMPTY";
    public const string ItemTypeInvalid = "SPOTIFY_ITEM_TYPE_INVALID";
    public const string NameTooLong = "SPOTIFY_ITEM_NAME_TOO_LONG";
    public const string ArtistNameTooLong = "SPOTIFY_ITEM_ARTIST_NAME_TOO_LONG";
    public const string AlbumNameTooLong = "SPOTIFY_ITEM_ALBUM_NAME_TOO_LONG";
    public const string ImgUrlTooLong = "SPOTIFY_ITEM_IMG_URL_TOO_LONG";
    public const string PreviewUrlTooLong = "SPOTIFY_ITEM_PREVIEW_URL_TOO_LONG";
}
