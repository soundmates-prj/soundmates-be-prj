using AccountContentService.Application.Abstractions;
using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPopularPosts;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace AccountContentService.Application.Features.BlogComments.Queries.GetComments
{
    public class GetCommentsHandler : 
        IRequestHandler<GetCommentsQuery, PaginationResult<CommentDto>>,
        IRequestHandler<GetUserCommentsQuery, PaginationResult<CommentDto>>,
        IRequestHandler<GetCommentDetailQuery, List<CommentDto>>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IMapper _mapper;
        private readonly IUserServiceClient _userClient;

        public GetCommentsHandler(ICommentRepository commentRepository, IMapper mapper, IUserServiceClient userClient)
        {
            _commentRepository = commentRepository;
            _mapper = mapper;
            _userClient = userClient;
        }

        public async Task<PaginationResult<CommentDto>> Handle(
             GetCommentsQuery request,
             CancellationToken cancellationToken)
        {
            var result = await _commentRepository.GetByPostIdAsync(request.PostId, request.PageSize, request.Page, cancellationToken);
            var items = _mapper.Map<IEnumerable<CommentDto>>(result.Items).ToList();
            var comments = BuildCommentTree(items);
            

            return new PaginationResult<CommentDto>
            {
                Items = comments,
                TotalCount = result.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<PaginationResult<CommentDto>> Handle(
             GetUserCommentsQuery request,
             CancellationToken cancellationToken)
        {
            var result = await _commentRepository.GetByUserIdAsync(request.UserId, request.PageSize, request.Page, cancellationToken);
            var items = _mapper.Map<IEnumerable<CommentDto>>(result.Items).ToList();
            var comments = BuildCommentTree(items);

            return new PaginationResult<CommentDto>
            {
                Items = comments,
                TotalCount = result.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<List<CommentDto>> Handle(
            GetCommentDetailQuery request,
            CancellationToken cancellationToken)
        {
            var reuslt = await _commentRepository.GetDetailByIdAsync(request.CommentId, cancellationToken);
            var items = _mapper.Map<List<CommentDto>>(reuslt);
            var commentDto = BuildCommentTree(items);  
            return commentDto;
        }


        private List<CommentDto> BuildCommentTree(List<CommentDto> comments)
        {
            var lookup = comments.ToDictionary(x => x.Id);

            var roots = new List<CommentDto>();

            foreach (var comment in comments)
            {
                if (comment.ParentCommentId == null ||
                    comment.ParentCommentId == Guid.Empty)
                {
                    roots.Add(comment);
                    continue;
                }

                if (lookup.TryGetValue(comment.ParentCommentId, out var parent))
                {
                    parent.Replies.Add(comment);
                }
                else
                {
                    roots.Add(comment);
                }
            }

            return roots;
        }
    }
}
