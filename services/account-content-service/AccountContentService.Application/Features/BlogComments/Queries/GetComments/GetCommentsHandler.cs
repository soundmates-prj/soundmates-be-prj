using AccountContentService.Application.Abstractions;
using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;

namespace AccountContentService.Application.Features.BlogComments.Queries.GetComments
{
    public class GetCommentsHandler :
        IRequestHandler<GetCommentsQuery, PaginationResult<CommentDto>>,
        IRequestHandler<GetUserCommentsQuery, PaginationResult<CommentDto>>,
        IRequestHandler<GetCommentDetailQuery, List<CommentDto>>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IUserProfileCache _userProfileCache;
        private readonly IMapper _mapper;

        public GetCommentsHandler(
            ICommentRepository commentRepository,
            IUserProfileCache userProfileCache,
            IMapper mapper)
        {
            _commentRepository = commentRepository;
            _userProfileCache = userProfileCache;
            _mapper = mapper;
        }

        public async Task<PaginationResult<CommentDto>> Handle(
             GetCommentsQuery request,
             CancellationToken cancellationToken)
        {
            var result = await _commentRepository.GetByPostIdAsync(request.PostId, request.PageSize, request.Page, cancellationToken);
            var items = _mapper.Map<IEnumerable<CommentDto>>(result.Items).ToList();
            await PopulateUserProfilesAsync(items, cancellationToken);
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
            await PopulateUserProfilesAsync(items, cancellationToken);
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
            var result = await _commentRepository.GetDetailByIdAsync(request.CommentId, cancellationToken);
            var items = _mapper.Map<List<CommentDto>>(result);
            await PopulateUserProfilesAsync(items, cancellationToken);
            return BuildCommentTree(items);
        }

        /// <summary>
        /// Refreshes userFullName and userAvatarUrl from the local read-model
        /// projection for every comment in the list (including replies).
        /// </summary>
        private async Task PopulateUserProfilesAsync(List<CommentDto> comments, CancellationToken ct)
        {
            foreach (var comment in comments)
            {
                await PopulateUserProfileAsync(comment, ct);
                if (comment.Replies.Count > 0)
                    await PopulateUserProfilesAsync(comment.Replies, ct);
            }
        }

        private async Task PopulateUserProfileAsync(CommentDto comment, CancellationToken ct)
        {
            if (comment.UserId == Guid.Empty) return;
            var profile = await _userProfileCache.GetProfileAsync(comment.UserId, ct);
            comment.UserFullName = profile.FullName;
            comment.UserAvatarUrl = profile.AvatarUrl ?? string.Empty;
        }

        private List<CommentDto> BuildCommentTree(List<CommentDto> comments)
        {
            var lookup = comments.ToDictionary(x => x.Id);
            var roots = new List<CommentDto>();

            foreach (var comment in comments)
            {
                if (comment.ParentCommentId == Guid.Empty)
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
