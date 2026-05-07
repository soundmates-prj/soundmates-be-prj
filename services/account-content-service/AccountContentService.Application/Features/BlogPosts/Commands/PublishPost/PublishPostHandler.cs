using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Commands.PublishPost
{
    public class PublishPostHandler
    : IRequestHandler<PublishPostCommand, bool>
    {
        private readonly IBlogPostRepository _repository;
        private readonly IBlogReportRepository _reportRepository;

        public PublishPostHandler(IBlogPostRepository repository, IBlogReportRepository reportRepository)
        {
            _repository = repository;
            _reportRepository = reportRepository;
        }

        public async Task<bool> Handle(
            PublishPostCommand request,
            CancellationToken cancellationToken)
        {
            var post = await _repository.GetByIdAsync(request.PostId, cancellationToken);

            if (post == null)
            {
                throw new NotFoundException("Post not found");
            }

            if (post.Status.ToLower().Equals(PostStatus.Banned.ToString().ToLower()))
            {
                await _reportRepository.DeleteReportsByPostIdAsync(request.PostId, cancellationToken);
            }

            post.Status = PostStatus.Published.ToString();
            post.IsActive = true;
            post.PublishedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(post);

            return true;
        }
    }
}
