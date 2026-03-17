using System;
using System.Collections.Generic;
using System.Text;

namespace AuthService.Domain.Entities
{

    // <summary> Save Cache Spotify items (tracks, artists, albums) for quick retrieval and display in the app </summary>
    public partial class SpotifyItem
    {
        public Guid Id { get; set; }

        public string SpotifyId { get; set; } = null!;

        /// <summary>
        /// Type of Spotify item (e.g., "track", "artist", "album")
        /// </summary>
        public string ItemType { get; set; } = null!;

        /// <summary>
        /// name of the Spotify item (e.g., track name, artist name, album name)
        /// </summary>
        public string Name { get; set; } = null!;

        public string ArtistName { get; set; } = null!;

        public string AlbumName { get; set; } = null!;

        public string ImgUrl { get; set; } = null!;

        public string PreviewUrl { get; set; } = null!;

        public string RawJson { get; set; } = null!;

        public DateTime? UpdatedAt { get; set; }
    }
}
