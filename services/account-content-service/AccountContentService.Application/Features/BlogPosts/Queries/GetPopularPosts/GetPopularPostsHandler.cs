using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Features.BlogPosts.Queries.GetTrendingPosts;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Queries.GetPopularPosts
{
    public class GetPopularPostsHandler
    : IRequestHandler<GetPopularPostsQuery, PaginationResult<PopularPostsResponse>>
    {
        private readonly IBlogPostRepository _repository;

        public GetPopularPostsHandler(IBlogPostRepository repository)
        {
            _repository = repository;
        }

        public async Task<PaginationResult<PopularPostsResponse>> Handle(
            GetPopularPostsQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.GetPopularPostsAsync(request, cancellationToken);
        }
    }
}
