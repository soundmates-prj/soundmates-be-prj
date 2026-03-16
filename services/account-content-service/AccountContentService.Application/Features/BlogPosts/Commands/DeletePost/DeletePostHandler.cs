using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Commands.DeletePost
{
    public class DeletePostHandler : IRequestHandler<DeletePostCommand, bool>
    {
        private readonly IBlogPostRepository _repository;

        public DeletePostHandler(IBlogPostRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            DeletePostCommand request,
            CancellationToken cancellationToken)
        {
            var post = await _repository.GetByIdAsync(request.PostId, cancellationToken);

            if (post == null)
            {
                throw new NotFoundException("Post not found");
            }

            await _repository.DeleteAsync(post);

            return true;
        }
    }
}
