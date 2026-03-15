namespace AccountContentService.Api.Constants
{
    /// <summary>
    /// Centralized API route definitions for AccountContentService.
    /// Base route: /v1/api
    /// </summary>
    public static class ApiRoutes
    {
        private const string Base = "v1/api";

        /// <summary>
        /// Blog Post endpoints.
        /// </summary>
        public static class Posts
        {
            private const string BaseRoute = $"{Base}/posts";

            // ===============================
            // CRUD
            // ===============================

            /// <summary>
            /// POST /v1/api/posts
            /// Create a new blog post.
            /// </summary>
            public const string Create = BaseRoute;

            /// <summary>
            /// GET /v1/api/posts
            /// Retrieve all blog posts (feed).
            /// </summary>
            public const string GetAll = BaseRoute;

            /// <summary>
            /// GET /v1/api/posts/{postId}
            /// Retrieve blog post details.
            /// </summary>
            public const string GetById = $"{BaseRoute}/{{postId:guid}}";

            /// <summary>
            /// PUT /v1/api/posts/{postId}
            /// Update blog post content.
            /// </summary>
            public const string Update = $"{BaseRoute}/{{postId:guid}}";

            /// <summary>
            /// DELETE /v1/api/posts/{postId}
            /// Delete a blog post.
            /// </summary>
            public const string Delete = $"{BaseRoute}/{{postId:guid}}";

            // ===============================
            // PUBLISHED POSTS
            // ===============================

            /// <summary>
            /// GET /v1/api/posts/published
            /// Retrieve all published blog posts.
            /// </summary>
            public const string GetAllPublished = $"{BaseRoute}/published";

            /// <summary>
            /// GET /v1/api/posts/published/{postId}
            /// Retrieve details of a published blog post.
            /// </summary>
            public const string GetPublishedById = $"{BaseRoute}/published/{{postId:guid}}";

            // ===============================
            // POST LIFECYCLE
            // ===============================

            /// <summary>
            /// PATCH /v1/api/posts/{postId}/publish
            /// Publish a blog post.
            /// </summary>
            public const string Publish = $"{BaseRoute}/{{postId:guid}}/publish";

            /// <summary>
            /// PATCH /v1/api/posts/{postId}/draft
            /// Save or revert a blog post to draft status.
            /// </summary>
            public const string Draft = $"{BaseRoute}/{{postId:guid}}/draft";

            /// <summary>
            /// PATCH /v1/api/posts/{postId}/archive
            /// Archive a blog post.
            /// </summary>
            public const string Archive = $"{BaseRoute}/{{postId:guid}}/archive";

            // ===============================
            // DISCOVERY
            // ===============================

            /// <summary>
            /// GET /v1/api/posts/moods/{moodTag}
            /// Retrieve blog posts filtered by mood.
            /// </summary>
            public const string GetByMood = $"{BaseRoute}/moods/{{moodTag}}";

            /// <summary>
            /// GET /v1/api/posts/trending
            /// Retrieve trending blog posts.
            /// </summary>
            public const string Trending = $"{BaseRoute}/trending";

            /// <summary>
            /// GET /v1/api/posts/popular
            /// Retrieve popular blog posts.
            /// </summary>
            public const string Popular = $"{BaseRoute}/popular";

            /// <summary>
            /// GET /v1/api/posts/recommended
            /// Retrieve recommended blog posts.
            /// </summary>
            public const string Recommended = $"{BaseRoute}/recommended";

            /// <summary>
            /// GET /v1/api/posts/search
            /// Search blog posts by keyword.
            /// </summary>
            public const string Search = $"{BaseRoute}/search";

            // ===============================
            // MEDIA
            // ===============================

            /// <summary>
            /// POST /v1/api/posts/{postId}/cover-image
            /// Upload or update cover image of the blog post.
            /// </summary>
            public const string UploadCoverImage = $"{BaseRoute}/{{postId:guid}}/cover-image";

            /// <summary>
            /// POST /v1/api/posts/{postId}/audio
            /// Upload audio narration for the blog post.
            /// </summary>
            public const string UploadAudio = $"{BaseRoute}/{{postId:guid}}/audio";

            /// <summary>
            /// POST /v1/api/posts/{postId}/audio/generate
            /// Generate AI audio narration from blog content.
            /// </summary>
            public const string GenerateAudio = $"{BaseRoute}/{{postId:guid}}/audio/generate";

            /// <summary>
            /// GET /v1/api/posts/{postId}/audio
            /// Retrieve audio narration of the blog post.
            /// </summary>
            public const string GetAudio = $"{BaseRoute}/{{postId:guid}}/audio";

            /// <summary>
            /// DELETE /v1/api/posts/{postId}/audio
            /// Remove audio narration from the blog post.
            /// </summary>
            public const string DeleteAudio = $"{BaseRoute}/{{postId:guid}}/audio";

            // ===============================
            // STATISTICS
            // ===============================

            /// <summary>
            /// GET /v1/api/posts/{postId}/stats
            /// Retrieve engagement statistics of the blog post.
            /// </summary>
            public const string GetStats = $"{BaseRoute}/{{postId:guid}}/stats";

            /// <summary>
            /// POST /v1/api/posts/{postId}/views
            /// Increase the view count of the blog post.
            /// </summary>
            public const string IncreaseViews = $"{BaseRoute}/{{postId:guid}}/views";
        }

        // ===============================
        // USER POSTS
        // ===============================

        public static class Users
        {
            private const string BaseRoute = $"{Base}/users";

            /// <summary>
            /// GET /v1/api/users/{userId}/posts
            /// Retrieve blog posts created by a specific user.
            /// </summary>
            public const string GetUserPosts = $"{BaseRoute}/{{userId:guid}}/posts";
        }

        public static class Me
        {
            private const string BaseRoute = $"{Base}/me";

            /// <summary>
            /// GET /v1/api/me/posts
            /// Retrieve blog posts created by the current user.
            /// </summary>
            public const string MyPosts = $"{BaseRoute}/posts";
        }
    }
}