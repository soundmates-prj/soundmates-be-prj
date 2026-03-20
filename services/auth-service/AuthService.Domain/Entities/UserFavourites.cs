using System;
using System.Collections.Generic;
using System.Text;

namespace AuthService.Domain.Entities
{
    // <summary> Favorite items for a user, such as tracks, artists, albums, blog, podcast, schedule etc. </summary>
    public partial class UserFavourite
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        /// <summary>
        /// Type of the item (e.g., "track", "artist", "album")
        /// </summary>
        public string ItemType { get; set; } = null!;

        /// <summary>
        /// Spotify ID of the item (e.g., track ID, artist ID, album ID)
        /// </summary>
        public string ItemId { get; set; } = null!; // spotify id

        /// <summary>
        /// Source from which the item was added to favourites (e.g., "spotify", "local", "other")
        /// </summary>
        public string Source { get; set; } = null!; // spotify / local / other

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
