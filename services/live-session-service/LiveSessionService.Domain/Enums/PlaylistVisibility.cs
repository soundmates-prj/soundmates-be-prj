namespace LiveSessionService.Domain.Enums
{
    public enum PlaylistVisibility
    {
        Public = 0,
        Private = 1,

        // Không xuất hiện khi tìm kiếm, nhưng ai có Link trực tiếp thì vẫn xem được
        // Giống tính năng "Unlisted" của YouTube
        Unlisted = 2
    }
}
