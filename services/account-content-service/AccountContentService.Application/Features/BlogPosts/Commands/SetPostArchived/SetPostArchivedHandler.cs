using AccountContentService.Application.Abstractions;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Features.BlogPosts.Commands.SetPostDraft;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Commands.SetPostArchived
{
    public class SetPostArchivedHandler : IRequestHandler<SetPostArchivedCommand, bool>
    {
        private readonly IBlogPostRepository _repository;

        public SetPostArchivedHandler(IBlogPostRepository repository)
        {
            _repository = repository;
        }
        public async Task<bool> Handle(
            SetPostArchivedCommand request,
            CancellationToken cancellationToken)
        {
            var post = await _repository.GetByIdAsync(request.PostId, cancellationToken);

            if (post == null)
            {
                throw new NotFoundException("Post not found");
            }

            post.Status = PostStatus.Archived.ToString();
            post.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(post);

            return true;
        }
    }
}
