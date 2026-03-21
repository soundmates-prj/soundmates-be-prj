using AccountContentService.Api.Common;
using AccountContentService.Api.Constants;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.Features.BlogPosts.Commands.CreatePost;
using AccountContentService.Application.Features.BlogPosts.Commands.DeletePost;
using AccountContentService.Application.Features.BlogPosts.Commands.PublishPost;
using AccountContentService.Application.Features.BlogPosts.Commands.ShareMusicPost;
using AccountContentService.Application.Features.BlogPosts.Commands.SetPostArchived;
using AccountContentService.Application.Features.BlogPosts.Commands.SetPostDraft;
using AccountContentService.Application.Features.BlogPosts.Commands.UpdatePost;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPopularPosts;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPosts;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPostStats;
using AccountContentService.Application.Features.BlogPosts.Queries.GetTrendingPosts;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sprache;


[ApiController]
[Authorize]
public class BlogController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public BlogController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>
    /// Create a new blog post
    /// </summary>
    /// <remarks>
    /// Creates a new blog post for the authenticated user.
    /// </remarks>
    /// <response code="200">Create post successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPost(ApiRoutes.Posts.Create)]
    public async Task<IActionResult> CreatePost(CreatePostRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);

        var command = _mapper.Map<CreatePostCommand>(request);
        command.UserId = userId;

        var result = await _mediator.Send(command);
        var response = _mapper.Map<PostResponse>(result);

        return Ok(ApiResponse<PostResponse>.Ok(response, "Create post successfully"));
    }

    /// <summary>
    /// Share a music card to current user's blog wall.
    /// </summary>
    [HttpPost(ApiRoutes.Posts.ShareMusic)]
    public async Task<IActionResult> ShareMusicPost(ShareMusicPostRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);

        var command = _mapper.Map<ShareMusicPostCommand>(request);
        command.UserId = userId;

        var result = await _mediator.Send(command);
        var response = _mapper.Map<PostResponse>(result);

        return Ok(ApiResponse<PostResponse>.Ok(response, "Share music post successfully"));
    }

    /// <summary>
    /// Update an existing blog post
    /// </summary>
    /// <param name="postId">Post identifier</param>
    /// <param name="request">Post request body</param>
    /// <response code="200">Update post successfully</response>
    /// <response code="404">Post not found</response>
    [HttpPut(ApiRoutes.Posts.Update)]
    public async Task<IActionResult> UpdatePost(
        [FromRoute] Guid postId,
        UpdatePostRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);

        var command = _mapper.Map<UpdatePostCommand>(request);
        command.PostId = postId;

        var result = await _mediator.Send(command);
        var response = _mapper.Map<PostResponse>(result);

        return Ok(ApiResponse<PostResponse>.Ok(response, "Update post successfully"));
    }

    /// <summary>
    /// Retrieve all blog posts
    /// </summary>
    /// <remarks>
    /// Returns paginated list of all blog posts.
    /// Filters can be applied using query parameters (e.g., status, moodTag).
    /// </remarks>
    /// <response code="200">Get posts successfully</response>
    [HttpGet(ApiRoutes.Posts.GetAll)]
    public async Task<IActionResult> GetPosts([FromQuery] PaginationRequest request)
    {
        var query = _mapper.Map<GetPostsQuery>(request);

        var result = await _mediator.Send(query);

        var items = _mapper.Map<IEnumerable<PostResponse>>(result.Items);

        var response = new PaginationResponse<PostResponse>(
            items,
            result.Page,
            result.PageSize,
            result.TotalCount);

        return Ok(ApiResponse<PaginationResponse<PostResponse>>.Ok(response, "Get posts successfully"));
    }

    /// <summary>
    /// Retrieve all published blog posts
    /// </summary>
    /// <remarks>
    /// Returns paginated list of published blog posts only.
    /// Can be filtered by moodTag, authorName, search keyword, and date range using query parameters.
    /// </remarks>
    /// <response code="200">Get published posts successfully</response>
    [AllowAnonymous]
    [HttpGet(ApiRoutes.Posts.GetAllPublished)]
    public async Task<IActionResult> GetPublishedPosts([FromQuery] PaginationRequest request)
    {
        var query = _mapper.Map<GetPublisedPostsQuery>(request);

        var result = await _mediator.Send(query);

        var items = _mapper.Map<IEnumerable<PostResponse>>(result.Items);

        var response = new PaginationResponse<PostResponse>(
            items,
            result.Page,
            result.PageSize,
            result.TotalCount);

        return Ok(ApiResponse<PaginationResponse<PostResponse>>.Ok(response, "Get published posts successfully"));
    }

    /// <summary>
    /// Retrieve trending blog posts
    /// </summary>
    /// <remarks>
    /// Returns paginated list of trending blog posts.
    /// Can be filtered by moodTag, authorName, search keyword, and date range using query parameters.
    /// </remarks>
    /// <response code="200">Get trending posts successfully</response>
    [AllowAnonymous]
    [HttpGet(ApiRoutes.Posts.Trending)]
    public async Task<IActionResult> GetTrendingPosts(
  [FromQuery] PaginationRequest request)
    {
        var query = _mapper.Map<GetTrendingPostsQuery>(request);

        var result = await _mediator.Send(query);

        var response = new PaginationResponse<TrendingPostResponse>(
            result.Items,
            result.Page,
            result.PageSize,
            result.TotalCount);

        return Ok(ApiResponse<PaginationResponse<TrendingPostResponse>>.Ok(response, "Get trending posts successfully"));
    }

    /// <summary>
    /// Retrieve popular blog posts
    /// </summary>
    /// <remarks>
    /// Returns paginated list of popular blog posts.
    /// Can be filtered by moodTag, authorName, search keyword, and date range using query parameters.
    /// </remarks>
    /// <response code="200">Get popular posts successfully</response>
    [AllowAnonymous]
    [HttpGet(ApiRoutes.Posts.Popular)]
    public async Task<IActionResult> GetPopularPosts(
  [FromQuery] PaginationRequest request)
    {
        var query = _mapper.Map<GetPopularPostsQuery>(request);

        var result = await _mediator.Send(query);

        var response = new PaginationResponse<PopularPostsResponse>(
            result.Items,
            result.Page,
            result.PageSize,
            result.TotalCount);

        return Ok(ApiResponse<PaginationResponse<PopularPostsResponse>>.Ok(response, "Get popular posts successfully"));
    }

    /// <summary>
    /// Retrieve all stats of blog posts
    /// </summary>
    /// <remarks>
    /// Returns paginated list of popular blog posts.
    /// Can be filtered by moodTag, authorName, search keyword, and date range using query parameters.
    /// </remarks>
    /// <response code="200">Get statistics successfully</response>
    [HttpGet(ApiRoutes.Posts.GetAllStats)]
    public async Task<IActionResult> GetPostsStats([FromQuery] PaginationRequest request)
    {
        var query = _mapper.Map<GetPostsStatsQuery>(request);

        var result = await _mediator.Send(query);

        var response = new PaginationResponse<PostStatsResponse>(
            result.Items,
            result.Page,
            result.PageSize,
            result.TotalCount);

        return Ok(ApiResponse<PaginationResponse<PostStatsResponse>>.Ok(response, "Get statistics successfully"));
    }

    /// <summary>
    /// Retrieve a published blog post by id
    /// </summary>
    /// <param name="postId">Post identifier</param>
    /// <response code="200">Get post successfully</response>
    /// <response code="404">Post not found</response>
    [AllowAnonymous]
    [HttpGet(ApiRoutes.Posts.GetPublishedById)]
    public async Task<IActionResult> GetPublisedPostById([FromRoute] Guid postId)
    {
        var query = new GetPublisedPostDetailQuery(postId);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(ApiResponse<string>.Fail("Post not found"));
        }

        var response = _mapper.Map<PostResponse>(result);
        return Ok(ApiResponse<PostResponse>.Ok(response, "Get post successfully"));
    }

    /// <summary>
    /// Retrieve blog post details
    /// </summary>
    /// <param name="postId">Post identifier</param>
    /// <response code="200">Post retrieved successfully</response>
    /// <response code="404">Post not found</response>
    [HttpGet(ApiRoutes.Posts.GetById)]
    public async Task<IActionResult> GetPostById([FromRoute] Guid postId)
    {
        var query = new GetPostDetailQuery(postId);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(ApiResponse<string>.Fail("Post not found"));
        }

        var response = _mapper.Map<PostResponse>(result);
        return Ok(ApiResponse<PostResponse>.Ok(response, "Post retrieved successfully"));
    }

    /// <summary>
    /// Retrieve user blog post details
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="request"></param>
    /// <response code="200">Post retrieved successfully</response>
    /// <response code="404">Post not found</response>
    [HttpGet(ApiRoutes.Users.GetUserPosts)]
    public async Task<IActionResult> GetPostByUserId([FromRoute] Guid userId, [FromQuery]PaginationRequest request)
    {
        var query = new GetUserPostDetailQuery(userId, request.Page, request.PageSize);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(ApiResponse<string>.Fail("Post not found"));
        }
        var items = _mapper.Map<IEnumerable<PostResponse>>(result.Items);

        var response = new PaginationResponse<PostResponse>(
            items,
            result.Page,
            result.PageSize,
            result.TotalCount);

        return Ok(ApiResponse<PaginationResponse<PostResponse>>.Ok(response, "Get posts successfully"));
    }

    /// <summary>
    /// Retrieve blog posts created by the current user.
    /// </summary>
    /// <response code="200">Post retrieved successfully</response>
    /// <response code="404">Post not found</response>
    [HttpGet(ApiRoutes.Me.MyPosts)]
    public async Task<IActionResult> GetCurrentUserPost([FromQuery] PaginationRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var query = new GetUserPostDetailQuery(userId, request.Page, request.PageSize);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(ApiResponse<string>.Fail("Post not found"));
        }
        var items = _mapper.Map<IEnumerable<PostResponse>>(result.Items);

        var response = new PaginationResponse<PostResponse>(
            items,
            result.Page,
            result.PageSize,
            result.TotalCount);

        return Ok(ApiResponse<PaginationResponse<PostResponse>>.Ok(response, "Get posts successfully"));
    }

    /// <summary>
    /// Retrieve engagement statistics of the blog post.
    /// </summary>
    /// <param name="postId">User identifier</param> 
    /// <response code="200">Statistics retrieved successfully</response>
    /// <response code="404">Post not found</response>
    [HttpGet(ApiRoutes.Posts.GetStats)]
    public async Task<IActionResult> GetPostStats([FromRoute] Guid postId)
    {
        var query = new GetPostStatsQuery(postId);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(ApiResponse<string>.Fail("Post not found"));
        }

        return Ok(ApiResponse<PostStatsResponse>.Ok(result, "Statics retrieved successfully"));
    }

    /// <summary>
    /// Delete a blog post
    /// </summary>
    /// <param name="postId">Post identifier</param>
    [HttpDelete(ApiRoutes.Posts.Delete)]
    public async Task<IActionResult> DeletePostById([FromRoute] Guid postId)
    {
        var command = new DeletePostCommand(postId);
        var result = await _mediator.Send(command);

        return Ok(ApiResponse<bool>.Ok(result, $"Status: {result}"));
    }

    /// <summary>
    /// Publish a blog post
    /// </summary>
    /// <remarks>
    /// Changes the post status from Draft to Published.
    /// </remarks>
    /// <param name="postId">Post identifier</param>
    /// <response code="200">Post published successfully</response>
    /// <response code="404">Post not found</response>
    [HttpPatch(ApiRoutes.Posts.Publish)]
    public async Task<IActionResult> PublishPost([FromRoute] Guid postId)
    {
        var result = await _mediator.Send(new PublishPostCommand(postId));

        return Ok(ApiResponse<bool>.Ok(result, $"Status: {result}"));
    }

    /// <summary>
    /// Revert a blog post to draft status
    /// </summary>
    /// <remarks>
    /// Changes the post status to Draft (except Archive).
    /// </remarks>
    /// <param name="postId">Post identifier</param>
    /// <response code="200">Post published successfully</response>
    /// <response code="404">Post not found</response>
    [HttpPatch(ApiRoutes.Posts.Draft)]
    public async Task<IActionResult> MoveToDraft([FromRoute] Guid postId)
    {

        var result = await _mediator.Send(new SetPostDraftCommand(postId));

        return Ok(ApiResponse<bool>.Ok(result, $"Status: {result}"));
    }

    /// <summary>
    /// Archive a blog post
    /// </summary>
    /// <remarks>
    /// Archive a blog post.
    /// </remarks>
    /// <param name="postId">Post identifier</param>
    /// <response code="200">Post published successfully</response>
    /// <response code="404">Post not found</response>
    [HttpPatch(ApiRoutes.Posts.Archive)]
    public async Task<IActionResult> MoveToArchived([FromRoute] Guid postId)
    {

        var result = await _mediator.Send(new SetPostArchivedCommand(postId));

        return Ok(ApiResponse<bool>.Ok(result, $"Status: {result}"));
    }

  
}