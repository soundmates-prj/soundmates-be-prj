using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Common.Result;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AutoMapper;
using MediatR;

namespace AccountContentService.Application.Features.BlogPosts.Queries.GetPosts;

public class GetPostsHandler: 
    IRequestHandler<GetPostsQuery, PaginationResult<PostDto>>,
    IRequestHandler<GetPublisedPostsQuery, PaginationResult<PostDto>>,
    IRequestHandler<GetPostDetailQuery, PostDto>,
    IRequestHandler<GetPublisedPostDetailQuery, PostDto>,
    IRequestHandler<GetUserPostDetailQuery, PaginationResult<PostDto>>,
    IRequestHandler<GetCurrentUserPostDetailQuery, PaginationResult<PostDto>>
{
    private readonly IBlogPostRepository _repository;
    private readonly IMapper _mapper;

    public GetPostsHandler(
        IBlogPostRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PaginationResult<PostDto>> Handle(
        GetPostsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _repository.GetAllPostsAsync(request, cancellationToken);

        var items = _mapper.Map<IEnumerable<PostDto>>(result.Items);

        return new PaginationResult<PostDto>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<PaginationResult<PostDto>> Handle(
        GetPublisedPostsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _repository.GetAllPublishedPostsAsync(request, cancellationToken);

        var items = _mapper.Map<IEnumerable<PostDto>>(result.Items);

        return new PaginationResult<PostDto>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<PostDto> Handle(
        GetPostDetailQuery request,
        CancellationToken cancellationToken)
    {
        var post = await _repository.GetByIdAsync(request.PostId, cancellationToken);
        if (post == null)
        {
            return null;
        }
        return _mapper.Map<PostDto>(post);
    }

    public async Task<PostDto> Handle(
        GetPublisedPostDetailQuery request,
        CancellationToken cancellationToken)
    {
        var post = await _repository.GetPublishByIdAsync(request.PostId, cancellationToken);
        if (post == null)
        {
            return null;
        }
        return _mapper.Map<PostDto>(post);
    }

    public async Task<PaginationResult<PostDto>> Handle(
       GetUserPostDetailQuery request,
       CancellationToken cancellationToken)
    {
        var result = await _repository.GetByUserIdAsync(request.UserId, request.PageSize, request.Page, cancellationToken);
        var items = _mapper.Map<IEnumerable<PostDto>>(result.Items);

        return new PaginationResult<PostDto>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<PaginationResult<PostDto>> Handle(
       GetCurrentUserPostDetailQuery request,
       CancellationToken cancellationToken)
    {
        var result = await _repository.GetByUserIdAsync(request.UserId, request.PageSize, request.Page, cancellationToken);
        var items = _mapper.Map<IEnumerable<PostDto>>(result.Items);

        return new PaginationResult<PostDto>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}