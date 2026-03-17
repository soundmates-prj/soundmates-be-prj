using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Features.BlogPosts.Queries.GetTrendingPosts;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Queries.GetPostStats
{
    public class GetPostStatsHandler
    : IRequestHandler<GetPostStatsQuery, PostStatsResponse>,
      IRequestHandler<GetPostsStatsQuery, PaginationResult<PostStatsResponse>>
    {
        private readonly IBlogPostRepository _repository;

        public GetPostStatsHandler(IBlogPostRepository repository)
        {
            _repository = repository;
        }

        public async Task<PostStatsResponse> Handle(
            GetPostStatsQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.GetPostStatsAsync(request.PostId);
        }

        public async Task<PaginationResult<PostStatsResponse>> Handle(
            GetPostsStatsQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.GetPostsStatsAsync(request, cancellationToken);
        }
    }
}
