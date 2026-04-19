using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Common.Result;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AutoMapper;
using MediatR;
using System.Xml.Linq;

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
    private readonly IUserProfileCache _userProfileCache;


    public GetPostsHandler(
        IBlogPostRepository repository,
        IMapper mapper,
        IUserProfileCache userProfileCache)
    {
        _repository = repository;
        _mapper = mapper;
        _userProfileCache = userProfileCache;
    }

    public async Task<PaginationResult<PostDto>> Handle(
        GetPostsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _repository.GetAllPostsAsync(request, cancellationToken);

        var items = _mapper.Map<IEnumerable<PostDto>>(result.Items);
        await PopulateUserProfilesAsync(items.ToList(), cancellationToken);

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
        await PopulateUserProfilesAsync(items.ToList(), cancellationToken);

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
            throw new NotFoundException($"Post with id {request.PostId} was not found.");
        }
        var dto = _mapper.Map<PostDto>(post);
        await PopulateUserProfileAsync(dto, cancellationToken);

        return dto;
    }

    public async Task<PostDto> Handle(
        GetPublisedPostDetailQuery request,
        CancellationToken cancellationToken)
    {
        var post = await _repository.GetPublishByIdAsync(request.PostId, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException($"Post with id {request.PostId} was not found.");
        }


        var dto = _mapper.Map<PostDto>(post);
        await PopulateUserProfileAsync(dto, cancellationToken);

        return dto;
    }

    public async Task<PaginationResult<PostDto>> Handle(
       GetUserPostDetailQuery request,
       CancellationToken cancellationToken)
    {
        var result = await _repository.GetPublishedByUserIdAsync(request.UserId, request.PageSize, request.Page, cancellationToken);
        var items = _mapper.Map<IEnumerable<PostDto>>(result.Items);
        await PopulateUserProfilesAsync(items.ToList(), cancellationToken);

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
        await PopulateUserProfilesAsync(items.ToList(), cancellationToken);

        return new PaginationResult<PostDto>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// Refreshes userFullName and userAvatarUrl from the local read-model
    /// projection for every comment in the list (including replies).
    /// </summary>
    private async Task PopulateUserProfilesAsync(List<PostDto> posts, CancellationToken ct)
    {
        foreach (var post in posts)
        {
            await PopulateUserProfileAsync(post, ct);
        }
    }

    private async Task PopulateUserProfileAsync(PostDto post, CancellationToken ct)
    {
        if (post.UserId == Guid.Empty) return;
        var profile = await _userProfileCache.GetProfileAsync(post.UserId, ct);
        post.UserFullName = profile.FullName;
        post.UserAvatarUrl = profile.AvatarUrl ?? string.Empty;
    }
}