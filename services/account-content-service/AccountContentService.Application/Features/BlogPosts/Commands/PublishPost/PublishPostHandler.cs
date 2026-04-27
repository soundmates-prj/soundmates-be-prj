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

        public PublishPostHandler(IBlogPostRepository repository)
        {
            _repository = repository;
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
                throw new InvalidOperationException("This post can not be published");
            }

            post.Status = PostStatus.Published.ToString();
            post.PublishedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(post);

            return true;
        }
    }
}
