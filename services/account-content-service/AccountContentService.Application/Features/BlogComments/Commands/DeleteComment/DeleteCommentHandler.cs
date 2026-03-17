using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Features.BlogPosts.Commands.DeletePost;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogComments.Commands.DeleteComment
{
    public class DeleteCommentHandler : IRequestHandler<DeleteCommentCommand, bool>
    {
        private readonly ICommentRepository _repository;

        public DeleteCommentHandler(ICommentRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            DeleteCommentCommand request,
            CancellationToken cancellationToken)
        {
            var post = await _repository.GetByIdAsync(request.CommentId, cancellationToken);

            if (post == null)
            {
                throw new NotFoundException("Post not found");
            }

            await _repository.DeleteAsync(post);

            return true;
        }
    }

}
