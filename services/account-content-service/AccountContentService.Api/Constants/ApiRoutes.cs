namespace AccountContentService.Api.Constants
{
    /// <summary>
    /// Centralized API route definitions for AccountContentService.
    /// Base route: /v1/api
    /// </summary>
    public static class ApiRoutes
    {
        /// <summary>
        /// Base API route prefix.
        /// </summary>
        private const string Base = "api/v1";

        // =====================================================
        // POSTS
        // =====================================================

        /// <summary>
        /// Endpoints for blog post management.
        /// </summary>
        public static class Posts
        {
            private const string BaseRoute = $"{Base}/posts";

            // ===============================
            // CRUD
            // ===============================

            /// <summary>Create a new blog post.</summary>
            public const string Create = BaseRoute;

            /// <summary>Retrieve all blog posts.</summary>
            public const string GetAll = BaseRoute;

            /// <summary>Retrieve blog post details.</summary>
            public const string GetById = $"{BaseRoute}/{{postId:guid}}";

            /// <summary>Update blog post content.</summary>
            public const string Update = $"{BaseRoute}/{{postId:guid}}";

            /// <summary>Delete a blog post.</summary>
            public const string Delete = $"{BaseRoute}/{{postId:guid}}";

            // ===============================
            // PUBLISHED POSTS
            // ===============================

            /// <summary>Retrieve all published blog posts.</summary>
            public const string GetAllPublished = $"{BaseRoute}/published";

            /// <summary>Retrieve details of a published blog post.</summary>
            public const string GetPublishedById = $"{BaseRoute}/published/{{postId:guid}}";

            // ===============================
            // POST LIFECYCLE
            // ===============================

            /// <summary>Publish a blog post.</summary>
            public const string Publish = $"{BaseRoute}/{{postId:guid}}/publish";

            /// <summary>Save or revert a blog post to draft.</summary>
            public const string Draft = $"{BaseRoute}/{{postId:guid}}/draft";

            /// <summary>Archive a blog post.</summary>
            public const string Archive = $"{BaseRoute}/{{postId:guid}}/archive";

            // ===============================
            // DISCOVERY
            // ===============================

            /// <summary>Retrieve posts filtered by mood tag.</summary>
            public const string GetByMood = $"{BaseRoute}/moods/{{moodTag}}";

            /// <summary>Retrieve trending posts.</summary>
            public const string Trending = $"{BaseRoute}/trending";

            /// <summary>Retrieve popular posts.</summary>
            public const string Popular = $"{BaseRoute}/popular";

            /// <summary>Retrieve recommended posts for the current user.</summary>
            public const string Recommended = $"{BaseRoute}/recommended";

            /// <summary>Search blog posts by keyword.</summary>
            public const string Search = $"{BaseRoute}/search";

            // ===============================
            // MEDIA
            // ===============================

            /// <summary>Upload or update a blog post cover image.</summary>
            public const string UploadCoverImage = $"{BaseRoute}/{{postId:guid}}/cover-image";

            /// <summary>Upload audio narration.</summary>
            public const string UploadAudio = $"{BaseRoute}/{{postId:guid}}/audio";

            /// <summary>Generate AI narration from blog content.</summary>
            public const string GenerateAudio = $"{BaseRoute}/{{postId:guid}}/audio/generate";

            /// <summary>Retrieve blog post audio.</summary>
            public const string GetAudio = $"{BaseRoute}/{{postId:guid}}/audio";

            /// <summary>Delete blog post audio.</summary>
            public const string DeleteAudio = $"{BaseRoute}/{{postId:guid}}/audio";

            // ===============================
            // STATISTICS
            // ===============================

            /// <summary>Retrieve statistics of a specific post.</summary>
            public const string GetStats = $"{BaseRoute}/{{postId:guid}}/stats";

            /// <summary>Retrieve statistics of all posts.</summary>
            public const string GetAllStats = $"{BaseRoute}/stats";

            /// <summary>Increase post view count.</summary>
            public const string IncreaseViews = $"{BaseRoute}/{{postId:guid}}/views";
        }

        // =====================================================
        // USERS
        // =====================================================

        /// <summary>
        /// Endpoints related to user resources.
        /// </summary>
        public static class Users
        {
            private const string BaseRoute = $"{Base}/users";

            /// <summary>Retrieve blog posts created by a user.</summary>
            public const string GetUserPosts = $"{BaseRoute}/{{userId:guid}}/posts";

            /// <summary>Retrieve comments created by a user.</summary>
            public const string GetUserComments = $"{BaseRoute}/{{userId:guid}}/comments";

            /// <summary>Retrieve reactions created by a user.</summary>
            public const string GetUserReactions = $"{BaseRoute}/{{userId:guid}}/reactions";
        }

        // =====================================================
        // CURRENT USER
        // =====================================================

        /// <summary>
        /// Endpoints for the currently authenticated user.
        /// </summary>
        public static class Me
        {
            private const string BaseRoute = $"{Base}/me";

            /// <summary>Retrieve current user's posts.</summary>
            public const string MyPosts = $"{BaseRoute}/posts";

            /// <summary>Retrieve current user's comments.</summary>
            public const string MyComments = $"{BaseRoute}/comments";

            /// <summary>Retrieve current user's reactions.</summary>
            public const string MyReactions = $"{BaseRoute}/reactions";

            /// <summary>Retrieve current user's subscription.</summary>
            public const string MySubscription = $"{BaseRoute}/subscriptions";

            /// <summary>Retrieve current user's subscription history.</summary>
            public const string MySubscriptionHistory = $"{BaseRoute}/subscriptions/history";
        }

        // =====================================================
        // COMMENTS
        // =====================================================

        /// <summary>
        /// Endpoints for comment management.
        /// </summary>
        public static class Comments
        {
            private const string BaseRoute = $"{Base}/comments";

            /// <summary>Add a comment to a post.</summary>
            public const string Create = $"{Base}/posts/{{postId:guid}}/comments";

            /// <summary>Reply to an existing comment.</summary>
            public const string Reply = $"{BaseRoute}/{{commentId:guid}}/reply";

            /// <summary>Retrieve comments of a post.</summary>
            public const string GetByPost = $"{Base}/posts/{{postId:guid}}/comments";

            /// <summary>Retrieve comment details.</summary>
            public const string GetById = $"{BaseRoute}/{{commentId:guid}}";

            /// <summary>Update a comment.</summary>
            public const string Update = $"{BaseRoute}/{{commentId:guid}}";

            /// <summary>Delete a comment.</summary>
            public const string Delete = $"{BaseRoute}/{{commentId:guid}}";

            /// <summary>Report a comment.</summary>
            public const string Report = $"{BaseRoute}/{{commentId:guid}}/report";
        }

        // =====================================================
        // REACTIONS
        // =====================================================

        /// <summary>
        /// Endpoints for reactions.
        /// </summary>
        public static class Reactions
        {
            private const string BaseRoute = $"{Base}/reactions";

            /// <summary>Add reaction to a post.</summary>
            public const string Add = $"{Base}/posts/{{postId:guid}}/reactions";
            /// <summary>Update reaction.</summary>
            public const string Update = $"{Base}/reactions/{{reactionId:guid}}";

            /// <summary>Remove reaction.</summary>
            public const string Remove = $"{Base}/posts/{{postId:guid}}/reactions";

            /// <summary>Retrieve reaction summary.</summary>
            public const string GetSummary = $"{Base}/posts/{{postId:guid}}/reactions";

            /// <summary>Retrieve users who reacted.</summary>
            public const string GetUsers = $"{Base}/posts/{{postId:guid}}/reactions/users";
        }

        // =====================================================
        // SUBSCRIPTIONS
        // =====================================================

        /// <summary>
        /// Endpoints for subscription management.
        /// </summary>
        public static class Subscriptions
        {
            private const string BaseRoute = $"{Base}/subscriptions";

            /// <summary>Create new subscription plans.</summary>
            public const string CreatePlan = $"{Base}/subscription-plans";

            /// <summary>Update subscription plan.</summary>
            public const string UpdatePlan = $"{Base}/subscription-plans/{{planId:guid}}";

            /// <summary>Update subscription plan.</summary>
            public const string DeletePlan = $"{Base}/subscription-plans/{{planId:guid}}";

            /// <summary>Retrieve subscription plans.</summary>
            public const string GetPlans = $"{Base}/subscription-plans";

            /// <summary>Retrieve subscription plan detail.</summary>
            public const string GetPlanById = $"{Base}/subscription-plans/{{planId:guid}}";

            /// <summary>Create subscription.</summary>
            public const string Subscribe = BaseRoute;

            /// <summary>Check subscription status.</summary>
            public const string Status = $"{BaseRoute}/status";

            /// <summary>Cancel subscription.</summary>
            public const string Cancel = $"{BaseRoute}/{{subscriptionId:guid}}/cancel";

            /// <summary>Renew subscription.</summary>
            public const string Renew = $"{BaseRoute}/{{subscriptionId:guid}}/renew";
        }

        // =====================================================
        // PAYMENTS
        // =====================================================

        /// <summary>
        /// Endpoints for payment processing.
        /// </summary>
        public static class Payments
        {
            private const string BaseRoute = $"{Base}/payments";

            /// <summary>Create payment request.</summary>
            public const string Create = BaseRoute;

            /// <summary>Retrieve payment details.</summary>
            public const string GetById = $"{BaseRoute}/{{paymentId:guid}}";

            /// <summary>Retrieve payment history.</summary>
            public const string GetAll = BaseRoute;

            /// <summary>VNPay callback.</summary>
            public const string VNPayCallBack = $"{BaseRoute}/vnpay/callback";

            /// <summary>Payos webhook.</summary>
            public const string PayOsWebhook = $"{BaseRoute}/payos/webhook";

            /// <summary>Confirm payment.</summary>
            public const string Confirm = $"{BaseRoute}/{{paymentId:guid}}/confirm";

            /// <summary>Refund payment.</summary>
            public const string Refund = $"{BaseRoute}/refund";

            /// <summary>Check transaction status.</summary>
            public const string Status = $"{BaseRoute}/status/{{transactionId}}";
        }
    }
}