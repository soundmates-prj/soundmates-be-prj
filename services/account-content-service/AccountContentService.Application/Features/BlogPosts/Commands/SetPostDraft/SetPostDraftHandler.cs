using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Features.BlogPosts.Commands.PublishPost;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Commands.SetPostDraft
{
    public class SetPostDraftHandler
    : IRequestHandler<SetPostDraftCommand, bool>
    {
        private readonly IBlogPostRepository _repository;

        public SetPostDraftHandler(IBlogPostRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            SetPostDraftCommand request,
            CancellationToken cancellationToken)
        {
            var post = await _repository.GetByIdAsync(request.PostId, cancellationToken);

            if (post == null)
            {
                throw new NotFoundException("Post not found");
            }

            if (post.Status.ToLower().Equals(PostStatus.Archived.ToString()))
            {
                throw new InvalidOperationException("Archived posts cannot be edited");
            }
            post.Status = PostStatus.Draft.ToString();
            post.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(post);

            return true;
        }
    }
}
