using System.Text.Json;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers;

public sealed class UserEmailVerifiedEventHandler : UserEventHandlerBase
{
    public override string EventType => "auth.user.email.verified";

    public UserEmailVerifiedEventHandler(IUserReadRepository repository, ILogger<UserEmailVerifiedEventHandler> logger)
        : base(repository, logger) { }

    protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
    {
        var userId = GetUserId(root);
        _logger.LogInformation("Processing user email verified event: {UserId}", userId);

        var existing = await _repository.GetByIdAsync(userId);
        if (existing is null)
        {
            _logger.LogWarning("User not found in MongoDB for email verified event: {UserId}", userId);
            return;
        }

        existing.IsVerified = true;
        existing.IsActive = true;
        existing.EmailVerifiedAt = EventPropertyExtractor.GetDateTimeProperty(root, "emailVerifiedAt", "EmailVerifiedAt");
        existing.UpdatedAt = DateTime.UtcNow;

        await _repository.UpsertAsync(existing);
        _logger.LogInformation("User email verified in MongoDB: {UserId}", userId);
    }
}
