using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Enums;
using MediatR;
using shared.Contracts.Events.Notifications;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AccountContentService.Application.Features.BlogReports.Commands;

public class BanReportedPostCommand : IRequest<bool>
{
    public Guid BlogPostId { get; set; }

    public BanReportedPostCommand(Guid blogPostId)
    {
        BlogPostId = blogPostId;
    }
}

public class BanReportedPostCommandHandler : IRequestHandler<BanReportedPostCommand, bool>
{
    private readonly IBlogPostRepository _postRepository;
    private readonly IMessageBusPublisher _eventBus;

    public BanReportedPostCommandHandler(IBlogPostRepository postRepository, IMessageBusPublisher eventBus)
    {
        _postRepository = postRepository;
        _eventBus = eventBus;
    }

    public async Task<bool> Handle(BanReportedPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.BlogPostId, cancellationToken);

        if (post == null)
            throw new NotFoundException("Blog post not found.");

        post.Status = PostStatus.Banned.ToString();
        post.IsActive = false; 
        post.UpdatedAt = DateTime.UtcNow;

        await _postRepository.UpdateAsync(post);

        var @event = new NotificationEvent
        {
            Title = "Post Banned",
            ReceiveUserId = post.UserId,
            ReferenceId = post.Id,
            Type = "post-banned",
            Message = "Your post has been banned due to multiple reports."
        };

        var payload = JsonSerializer.Serialize(@event);

        await _eventBus.PublishAsync(
            "notification.created",
            payload
        );

        return true;
    }
}
