using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Queries.GetTrendingPosts
{
    public class GetTrendingPostsHandler
    : IRequestHandler<GetTrendingPostsQuery, PaginationResult<TrendingPostResponse>>
    {
        private readonly IBlogPostRepository _repository;

        public GetTrendingPostsHandler(IBlogPostRepository repository)
        {
            _repository = repository;
        }

        public async Task<PaginationResult<TrendingPostResponse>> Handle(
            GetTrendingPostsQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.GetTrendingPostsAsync(request, cancellationToken);
        }
    }
}
