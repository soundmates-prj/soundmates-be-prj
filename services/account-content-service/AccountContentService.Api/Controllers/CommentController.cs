using AccountContentService.Api.Common;
using AccountContentService.Api.Constants;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.Features.BlogComments.Commands.CreateComment;
using AccountContentService.Application.Features.BlogComments.Commands.DeleteComment;
using AccountContentService.Application.Features.BlogComments.Commands.ReplyComment;
using AccountContentService.Application.Features.BlogComments.Commands.UpdateComment;
using AccountContentService.Application.Features.BlogComments.Queries.GetComments;
using AccountContentService.Application.Features.BlogPosts.Commands.CreatePost;
using AccountContentService.Application.Features.BlogPosts.Commands.DeletePost;
using AccountContentService.Application.Features.BlogPosts.Commands.UpdatePost;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPosts;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AccountContentService.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class CommentController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public CommentController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        /// <summary>
        /// Add a comment to a blog post.
        /// </summary>
        /// <param name="postId">The ID of the blog post to comment on.</param>
        /// <param name="request">Comment body request</param>
        /// <remarks>
        /// Add a comment to a blog post for the authenticated user.
        /// </remarks>
        /// <response code="200">Comment post successfully</response>
        /// <response code="400">Invalid request</response>
        [HttpPost(ApiRoutes.Comments.Create)]
        public async Task<IActionResult> CreateComment([FromRoute] Guid postId, CommentRequest request)
        {
            var userId = UserContext.GetUserId(HttpContext);

            var command = _mapper.Map<CreateCommentCommand>(request);
            command.PostId = postId;
            command.UserId = userId;

            var result = await _mediator.Send(command);
            var response = _mapper.Map<CommentResponse>(result);

            return Ok(ApiResponse<CommentResponse>.Ok(response, "Create post successfully"));
        }

        /// <summary>
        /// Update a comment.
        /// </summary>
        /// <param name="commentId">Comment identifier</param>
        /// <param name="request">Comment request body</param>
        /// <response code="200">Update comment successfully</response>
        /// <response code="404">Comment not found</response>
        [HttpPut(ApiRoutes.Comments.Update)]
        public async Task<IActionResult> UpdateComment(
            [FromRoute] Guid commentId,
            CommentRequest request)
        {
            var userId = UserContext.GetUserId(HttpContext);

            var command = _mapper.Map<UpdateCommentCommand>(request);
            command.Id = commentId;

            var result = await _mediator.Send(command);
            var response = _mapper.Map<CommentResponse>(result);

            return Ok(ApiResponse<CommentResponse>.Ok(response, "Update comment successfully"));
        }

        /// <summary>
        /// Reply to an existing comment.
        /// </summary>
        /// <param name="commentId">The ID of the blog post to comment on.</param>
        /// <param name="request">Comment body request</param>
        /// <remarks>
        /// Reply to an existing comment to a blog post for the authenticated user.
        /// </remarks>
        /// <response code="200">Comment post successfully</response>
        /// <response code="400">Invalid request</response>
        [HttpPost(ApiRoutes.Comments.Reply)]
        public async Task<IActionResult> RelyComment([FromRoute] Guid commentId, CommentRequest request)
        {
            var userId = UserContext.GetUserId(HttpContext);
            var command = _mapper.Map<ReplyCommentCommand>(request);
            command.ParentCommentId = commentId;
            command.UserId = userId;

            var result = await _mediator.Send(command);
            var response = _mapper.Map<CommentResponse>(result);

            return Ok(ApiResponse<CommentResponse>.Ok(response, "Reply successfully"));
        }

        /// <summary>
        /// Delete a comment
        /// </summary>
        /// <param name="commentId">Comment identifier</param>
        [HttpDelete(ApiRoutes.Comments.Delete)]
        public async Task<IActionResult> DeletePostById([FromRoute] Guid commentId)
        {
            var command = new DeleteCommentCommand(commentId);
            var result = await _mediator.Send(command);

            return Ok(ApiResponse<bool>.Ok(result, $"Status: {result}"));
        }

        /// <summary>
        /// Retrieve comments of a blog post.
        /// </summary>
        /// <param name="postId">User identifier</param>
        /// <param name="request"></param>
        /// <response code="200">Get comments successfully</response>
        /// <response code="404">Post not found</response>
        [AllowAnonymous]
        [HttpGet(ApiRoutes.Comments.GetByPost)]
        public async Task<IActionResult> GetCommentByPostId([FromRoute] Guid postId, [FromQuery] PaginationNoFilterRequest request)
        {
            var query = _mapper.Map<GetCommentsQuery>(request);
            query.PostId = postId;
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("Post not found"));
            }
            var items = _mapper.Map<IEnumerable<CommentResponse>>(result.Items);

            var response = new PaginationResponse<CommentResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<CommentResponse>>.Ok(response, "Get comments successfully"));
        }

        /// <summary>
        /// Retrieve comments created by a specific user.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="request"></param>
        /// <response code="200">Get comments successfully</response>
        /// <response code="404">No comments found</response>
        [AllowAnonymous]
        [HttpGet(ApiRoutes.Users.GetUserComments)]
        public async Task<IActionResult> GetCommentByUserId([FromRoute] Guid userId, [FromQuery] PaginationNoFilterRequest request)
        {
            var query = _mapper.Map<GetUserCommentsQuery>(request);
            query.UserId = userId;
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("No comments found"));
            }
            var items = _mapper.Map<IEnumerable<CommentResponse>>(result.Items);

            var response = new PaginationResponse<CommentResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<CommentResponse>>.Ok(response, "Get comments successfully"));
        }

        /// <summary>
        /// Retrieve comments created by a current user.
        /// </summary>
        /// <param name="request"></param>
        /// <response code="200">Get comments successfully</response>
        /// <response code="404">No comments found</response>
        [HttpGet(ApiRoutes.Me.MyComments)]
        public async Task<IActionResult> GetPostByCurrentUser( [FromQuery] PaginationNoFilterRequest request)
        {
            var userId = UserContext.GetUserId(HttpContext);
            var query = _mapper.Map<GetUserCommentsQuery>(request);
            query.UserId = userId;
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("No comments found"));
            }
            var items = _mapper.Map<IEnumerable<CommentResponse>>(result.Items);

            var response = new PaginationResponse<CommentResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<CommentResponse>>.Ok(response, "Get comments successfully"));
        }

        /// <summary>
        /// Retrieve comment by Id.
        /// </summary>
        /// <param name="commentId">Comment identifier</param>
        /// <response code="200">Get comments successfully</response>
        /// <response code="404">No comments found</response>
        [AllowAnonymous]
        [HttpGet(ApiRoutes.Comments.GetById)]
        public async Task<IActionResult> GetCommentById([FromRoute] Guid commentId)
        {
            var query = new GetCommentDetailQuery(commentId);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("No comment found"));
            }
            var response = _mapper.Map<List<CommentResponse>>(result);

            return Ok(ApiResponse<List<CommentResponse>>.Ok(response, "Get comment successfully"));
        }
    }
}
