using AccountContentService.Api.Common;
using AccountContentService.Api.Constants;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.Features.SubscriptionPlans.Commands.CreatePlan;
using AccountContentService.Application.Features.SubscriptionPlans.Commands.UpdatePlan;
using AccountContentService.Application.Features.SubscriptionPlans.Commands.DeletePlan;
using AccountContentService.Application.Features.SubscriptionPlans.Queries.GetPlans;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AccountContentService.Application.Features.Subscriptions.Queries.GetSubscriptions;
using AccountContentService.Domain.Entities;

namespace AccountContentService.Api.Controllers
{
    /// <summary>
    /// Provides endpoints for subscription plans and user subscription history.
    /// </summary>
    /// <summary>
    /// Provides endpoints for subscription plans and user subscription history.
    /// </summary>
    [ApiController]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public SubscriptionController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        /// <summary>
        /// Create a new subscription plan.
        /// </summary>
        /// <remarks>
        /// This API allows administrators to create a new subscription plan 
        /// with pricing, duration, and feature configuration.
        /// </remarks>
        /// <response code="200">Subscription plan created successfully</response>
        /// <response code="400">Invalid request data</response>
        [HttpPost(ApiRoutes.Subscriptions.CreatePlan)]
        public async Task<IActionResult> CreateSubscriptionPlan(SubscriptionPlanRequest request)
        {
            var command = _mapper.Map<CreatePlanCommand>(request);

            var result = await _mediator.Send(command);
            var response = _mapper.Map<SubscriptionPlanResponse>(result);

            return Ok(ApiResponse<SubscriptionPlanResponse>.Ok(response, "Subscription plan created successfully"));
        }

        /// <summary>
        /// Update an existing subscription plan.
        /// </summary>
        /// <param name="planId">Subscription plan identifier</param>
        /// <param name="request">Updated subscription plan data</param>
        /// <response code="200">Subscription plan updated successfully</response>
        /// <response code="404">Subscription plan not found</response>
        [HttpPut(ApiRoutes.Subscriptions.UpdatePlan)]
        public async Task<IActionResult> UpdateSubscriptionPlan(
            [FromRoute] Guid planId,
            SubscriptionPlanRequest request)
        {
            var command = _mapper.Map<UpdatePlanCommand>(request);
            command.PlanId = planId;

            var result = await _mediator.Send(command);
            var response = _mapper.Map<SubscriptionPlanResponse>(result);

            return Ok(ApiResponse<SubscriptionPlanResponse>.Ok(response, "Subscription plan updated successfully"));
        }

        /// <summary>
        /// Delete a subscription plan.
        /// </summary>
        /// <param name="planId">Subscription plan identifier</param>
        /// <response code="200">Subscription plan deleted successfully</response>
        /// <response code="404">Subscription plan not found</response>
        [HttpDelete(ApiRoutes.Subscriptions.DeletePlan)]
        public async Task<IActionResult> DeleteSubscriptionPlan([FromRoute] Guid planId)
        {
            var command = new DeletePlanCommand(planId);
            var result = await _mediator.Send(command);

            return Ok(ApiResponse<bool>.Ok(result, result
                ? "Subscription plan deleted successfully"
                : "Subscription plan not found"));
        }

        /// <summary>
        /// Get subscription plan details by ID.
        /// </summary>
        /// <param name="planId">Subscription plan identifier</param>
        /// <response code="200">Subscription plan retrieved successfully</response>
        /// <response code="404">Subscription plan not found</response>
        [AllowAnonymous]
        [HttpGet(ApiRoutes.Subscriptions.GetPlanById)]
        public async Task<IActionResult> GetPlanById([FromRoute] Guid planId)
        {
            var query = new GetPlanDetailQuery(planId);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("Subscription plan not found"));
            }

            var response = _mapper.Map<SubscriptionPlanResponse>(result);

            return Ok(ApiResponse<SubscriptionPlanResponse>.Ok(response, "Subscription plan retrieved successfully"));
        }

        /// <summary>
        /// Get all available subscription plans.
        /// </summary>
        /// <response code="200">Subscription plans retrieved successfully</response>
        /// <response code="404">No subscription plans found</response>
        [AllowAnonymous]
        [HttpGet(ApiRoutes.Subscriptions.GetPlans)]
        public async Task<IActionResult> GetAllPlans()
        {
            var query = new GetPlansQuery();
            var result = await _mediator.Send(query);

            if (result == null || !result.Any())
            {
                return NotFound(ApiResponse<string>.Fail("No subscription plans found"));
            }

            var response = _mapper.Map<List<SubscriptionPlanResponse>>(result);

            return Ok(ApiResponse<List<SubscriptionPlanResponse>>.Ok(response, "Subscription plans retrieved successfully"));
        }

        /// <summary>
        /// Get current user's subscription history.
        /// </summary>
        /// <response code="200">Subscription retrieved successfully</response>
        /// <response code="404">Subscription not found</response>
        [HttpGet(ApiRoutes.Me.MySubscriptionHistory)]
        public async Task<IActionResult> GetCurrentUserSubscriptionHistory([FromQuery] PaginationNoFilterRequest request)
        {
            var userId = UserContext.GetUserId(HttpContext);
            var query = _mapper.Map<GetSubscriptionsHistoryQuery>(request);
            query.UserId = userId;
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("Subscription not found"));
            }
            var items = _mapper.Map<IEnumerable<SubscriptionResponse>>(result.Items);

            var response = new PaginationResponse<SubscriptionResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<SubscriptionResponse>>.Ok(response, "Subscription retrieved successfully"));
        }

        /// <summary>
        /// Get current user's subscription detail.
        /// </summary>
        /// <response code="200">Subscription retrieved successfully</response>
        /// <response code="404">Subscription not found</response>
        [HttpGet(ApiRoutes.Me.MySubscription)]
        public async Task<IActionResult> GetCurrentUserSubscription()
        {
            var userId = UserContext.GetUserId(HttpContext);
            var query = new GetUserSubscriptionQuery(userId);
          
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("Subscription not found"));
            }
            var response = _mapper.Map<SubscriptionResponse>(result);

            return Ok(ApiResponse<SubscriptionResponse>.Ok(response, "Subscription retrieved successfully"));
        }
    }
}