namespace Shared.Contracts;

/// <summary>
/// Centralized routing key constants for RabbitMQ topic exchange.
/// Matches the convention: [domain].[entity].[action]
///
/// Usage:
///   await _outbox.EnqueueAsync(RoutingKeys.Auth.User.Created, evt, ct);
/// </summary>
public static class RoutingKeys
{
    /// <summary>
    /// Auth service event routing keys.
    /// </summary>
    public static class Auth
    {
        public const string UserCreated           = "auth.user.created";
        public const string UserUpdated           = "auth.user.updated";
        public const string UserDeleted           = "auth.user.deleted";
        public const string UserActivated         = "auth.user.activated";
        public const string UserDeactivated       = "auth.user.deactivated";
        public const string UserBanned            = "auth.user.banned";
        public const string UserUnbanned          = "auth.user.unbanned";
        public const string UserEmailVerified      = "auth.user.email.verified";
        public const string UserProfileUpdated    = "auth.user.profile.updated";
        public const string RegistrationFailed     = "auth.user.registration.failed";
        public const string LoginSuccessful       = "auth.user.login.successful";
        public const string LoginFailed           = "auth.user.login.failed";
        public const string TokenRefreshed        = "auth.user.token.refreshed";
        public const string GoogleLoginSuccessful  = "auth.user.google.login.successful";
        public const string GoogleLoginFailed      = "auth.user.google.login.failed";
    }

    /// <summary>
    /// Config / infrastructure event routing keys.
    /// </summary>
    public static class Config
    {
        public const string AzuraCastUpdated = "config.azuracast.updated";
        public const string GeminiUpdated     = "config.gemini.updated";
    }
}
