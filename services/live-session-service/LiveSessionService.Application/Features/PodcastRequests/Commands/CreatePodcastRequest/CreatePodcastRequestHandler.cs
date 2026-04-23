using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.PodcastRequests.Commands.CreatePodcastRequest;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.PodcastRequests.Commands.CreatePodcastRequest;

public sealed class CreatePodcastRequestHandler
    : ICommandHandler<CreatePodcastRequestCommand, PodcastRequestResult>
{
    private readonly IPodcastRequestRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreatePodcastRequestHandler> _logger;

    public CreatePodcastRequestHandler(
        IPodcastRequestRepository repository,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreatePodcastRequestHandler> logger)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<PodcastRequestResult>> Handle(
        CreatePodcastRequestCommand command,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var podcastRequest = new Domain.Entities.PodcastRequest
        {
            Id = Guid.NewGuid(),
            RequestedByUserId = command.RequestedByUserId,
            AuthorInfo = command.AuthorInfo,
            Title = command.Title.Trim(),
            Type = command.Type.Trim(),
            Description = command.Description?.Trim(),
            BannerUrl = command.BannerUrl?.Trim(),
            Price = command.Price,
            IsPaid = command.IsPaid,
            Status = PodcastRequestStatus.Pending,
            RequestedAt = now
        };

        await _repository.AddAsync(podcastRequest, cancellationToken);

        _logger.LogInformation(
            "PodcastRequest {Id} created for series '{Title}' by user {UserId}",
            podcastRequest.Id, podcastRequest.Title, command.RequestedByUserId);

            object? parsedAuthorInfo = null;
            if (!string.IsNullOrWhiteSpace(podcastRequest.AuthorInfo))
            {
                var a = podcastRequest.AuthorInfo.Trim();
                if (a.Length > 0 && (a[0] == '{' || a[0] == '[' || a[0] == '"'))
                {
                    try { parsedAuthorInfo = System.Text.Json.JsonSerializer.Deserialize<object>(a); }
                    catch (Exception) { parsedAuthorInfo = a; }
                }
                else { parsedAuthorInfo = a; }
            }

            return Result<PodcastRequestResult>.Success(new PodcastRequestResult
            {
            Id = podcastRequest.Id,
            RequestedByUserId = podcastRequest.RequestedByUserId,
                AuthorInfo = parsedAuthorInfo,
            Title = podcastRequest.Title,
            Type = podcastRequest.Type,
            Description = podcastRequest.Description,
            BannerUrl = podcastRequest.BannerUrl,
            Price = podcastRequest.Price,
            IsPaid = podcastRequest.IsPaid,
            Status = podcastRequest.Status.ToString(),
            ReviewedByUserId = podcastRequest.ReviewedByUserId,
            ReviewedAt = podcastRequest.ReviewedAt,
            RejectReason = podcastRequest.RejectReason,
            RequestedAt = podcastRequest.RequestedAt
        });
    }
}