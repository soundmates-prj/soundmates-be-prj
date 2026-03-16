using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogComments.Commands.CreateComment
{
    public class CreateCommentHandler
     : IRequestHandler<CreateCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _repository;
        private readonly IBlogPostRepository _postRepository;
        private readonly IMapper _mapper;

        public CreateCommentHandler(
            ICommentRepository repository,
            IBlogPostRepository postRepository,
            IMapper mapper)
        {
            _repository = repository;
            _postRepository = postRepository;
            _mapper = mapper;
        }

        public async Task<CommentDto> Handle(
            CreateCommentCommand request,
            CancellationToken cancellationToken)
        {
            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);

            if (post == null)
            {
                throw new NotFoundException("Post not found");
            }

            var comment = _mapper.Map<BlogComment>(request);
            comment.Status = CommentStatus.Active.ToString();
            comment.CreatedAt  = DateTime.UtcNow;

            await _repository.AddAsync(comment);

            return _mapper.Map<CommentDto>(comment);
        }
    }
}
