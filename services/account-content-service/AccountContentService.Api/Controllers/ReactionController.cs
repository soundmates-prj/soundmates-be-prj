using AccountContentService.Api.Common;
using AccountContentService.Api.Constants;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.Features.BlogComments.Commands.CreateComment;
using AccountContentService.Application.Features.BlogPostReactions.Commands.CreateReaction;
using AccountContentService.Application.Features.BlogPostReactions.Commands.DeleteReaction;
using AccountContentService.Application.Features.BlogPostReactions.Commands.UpdateReaction;
using AccountContentService.Application.Features.BlogPostReactions.Queries.GetReactions;
using AccountContentService.Application.Features.BlogPosts.Commands.DeletePost;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AccountContentService.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class ReactionController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public ReactionController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }


        /// <summary>
        /// React to a blog post.
        /// </summary>
        /// <param name="postId">The ID of the blog post to react on.</param>
        /// <param name="request">Comment body request</param>
        /// <remarks>
        /// Add a reaction to a blog post.
        /// </remarks>
        /// <response code="200">React successfully</response>
        /// <response code="400">Invalid request</response>
        [HttpPost(ApiRoutes.Reactions.Add)]
        public async Task<IActionResult> CreateReaction([FromRoute] Guid postId, ReactionRequest request)
        {
            var userId = UserContext.GetUserId(HttpContext);

            var command = _mapper.Map<CreateReactionCommand>(request);
            command.PostId = postId;
            command.UserId = userId;

            var result = await _mediator.Send(command);
            var response = _mapper.Map<ReactionResponse>(result);

            return Ok(ApiResponse<ReactionResponse>.Ok(response, "React successfully"));
        }

        /// <summary>
        /// Change reaction.
        /// </summary>
        /// <param name="reactionId">The ID of the reaction.</param>
        /// <param name="request">Comment body request</param>
        /// <remarks>
        /// Add a reaction to a blog post.
        /// </remarks>
        /// <response code="200">Change React successfully</response>
        /// <response code="400">Invalid request</response>
        [HttpPut(ApiRoutes.Reactions.Update)]
        public async Task<IActionResult> UpdateReaction([FromRoute] Guid reactionId, ReactionRequest request)
        {
            var command = _mapper.Map<UpdateReactionCommand>(request);
            command.ReactionId = reactionId;

            var result = await _mediator.Send(command);
            var response = _mapper.Map<ReactionResponse>(result);

            return Ok(ApiResponse<ReactionResponse>.Ok(response, "Change React successfully"));
        }

        /// <summary>
        /// React to a blog post.
        /// </summary>
        /// <param name="postId">The ID of the blog post to comment on.</param>
        /// <remarks>
        /// Add a reaction to a blog post.
        /// </remarks>
        /// <response code="200">Change React successfully</response>
        /// <response code="400">Invalid request</response>
        [HttpGet(ApiRoutes.Reactions.GetUsers)]
        public async Task<IActionResult> GetReactions([FromRoute] Guid postId)
        {
            var query = new GetReactionsQuery(postId);

            var result = await _mediator.Send(query);
            var response = _mapper.Map<List<ReactionResponse>>(result);

            return Ok(ApiResponse<List<ReactionResponse>>.Ok(response, "Retrive successfully"));
        }

        /// <summary>
        /// Remove a reaction from a blog post.
        /// </summary>
        /// <param name="postId">The ID of the blog post to comment on.</param>
        /// <remarks>
        /// Add a reaction to a blog post.
        /// </remarks>
        /// <response code="200">Remove successfully</response>
        /// <response code="400">Invalid request</response>
        [HttpDelete(ApiRoutes.Reactions.Remove)]
        public async Task<IActionResult> DeleteReaction([FromRoute] Guid postId)
        {
            var userId = UserContext.GetUserId(HttpContext);
            var query = new DeleteReactionCommand(postId, userId);

            var result = await _mediator.Send(query);

            return Ok(ApiResponse<bool>.Ok(result, "Remove successfully"));
        }
    }
}
