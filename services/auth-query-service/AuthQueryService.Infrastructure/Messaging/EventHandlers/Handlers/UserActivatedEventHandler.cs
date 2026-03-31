using System.Text.Json;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers.Handlers;

public sealed class UserActivatedEventHandler : UserEventHandlerBase
{
    public override string EventType => "auth.user.activated";

    public UserActivatedEventHandler(IUserReadRepository repository, ILogger<UserActivatedEventHandler> logger)
        : base(repository, logger) { }

    protected override async Task HandleEventAsync(JsonElement root, CancellationToken cancellationToken)
    {
        var userId = GetUserId(root);
        _logger.LogInformation("Processing user activated event: {UserId}", userId);

        var existing = await _repository.GetByIdAsync(userId);
        if (existing is null)
        {
            _logger.LogWarning("User not found in MongoDB for activated event: {UserId}", userId);
            return;
        }

        existing.IsActive = true;
        existing.UpdatedAt = DateTime.UtcNow;

        await _repository.UpsertAsync(existing);
        _logger.LogInformation("User marked as ACTIVATED in MongoDB: {UserId}", userId);
    }
}
