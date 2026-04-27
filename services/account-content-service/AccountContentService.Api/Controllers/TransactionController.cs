using AccountContentService.Api.Common;
using AccountContentService.Api.Constants;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.Features.PaymentTransactions.Queries.GetTransactions;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountContentService.Api.Controllers
{
    /// <summary>
    /// Provides endpoints for querying payment transactions by id, user, and paging.
    /// </summary>
    /// <summary>
    /// Provides endpoints for querying payment transactions by id, user, and paging.
    /// </summary>
    [Authorize]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public TransactionController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }


        /// <summary>
        /// Get transaction by ID.
        /// </summary>
        /// <param name="transactionId">The unique identifier of the transaction</param>
        /// <response code="200">Transaction retrieved successfully</response>
        /// <response code="404">Transaction not found</response>
        [HttpGet(ApiRoutes.Transaction.GetById)]
        public async Task<IActionResult> GetTransactionById([FromRoute] Guid transactionId)
        {
            var query = new GetTransactionByIdQuery(transactionId);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("Transaction not found"));
            }
            var response = _mapper.Map<TransactionResponse>(result);

            return Ok(ApiResponse<TransactionResponse>.Ok(response, "Get transaction successfully"));
        }

        /// <summary>
        /// Get transaction by userId.
        /// </summary>
        /// <param name="userId">The user has success transactions</param>
        /// <param name="request">Pagination filter</param>
        /// <response code="200">Transaction retrieved successfully</response>
        /// <response code="404">Tystem transaction not found</response>
        [HttpGet(ApiRoutes.Users.GetUserTransactions)]
        public async Task<IActionResult> GetTransactionByUser([FromRoute] Guid userId, [FromQuery] PaginationNoFilterRequest request)
        {
            var query = _mapper.Map<GetTransactionByUserIdQuery>(request);
            query.UserId = userId;

            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("Transactions not found"));
            }
            var items = _mapper.Map<IEnumerable<TransactionResponse>>(result.Items);

            var response = new PaginationResponse<TransactionResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<TransactionResponse>>.Ok(response, "Get transactions successfully"));
        }

        /// <summary>
        /// Get current user's transaction.
        /// </summary>
        /// <param name="userId">The user has success transactions</param>
        /// <param name="request">Pagination filter</param>
        /// <response code="200">System transaction retrieved successfully</response>
        /// <response code="404">System transaction not found</response>
        [HttpGet(ApiRoutes.Me.MyTransctionHistory)]
        public async Task<IActionResult> GetMyTransactions([FromQuery] PaginationNoFilterRequest request)
        {
            var userId = UserContext.GetUserId(HttpContext);
            var query = _mapper.Map<GetTransactionByUserIdQuery>(request);
            query.UserId = userId;

            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("Transactions not found"));
            }
            var items = _mapper.Map<IEnumerable<TransactionResponse>>(result.Items);

            var response = new PaginationResponse<TransactionResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<TransactionResponse>>.Ok(response, "Get transactions successfully"));
        }

        /// <summary>
        /// Get all trasaction with pagination.
        /// </summary>
        /// <param name="request">Pagination parameters</param>
        /// <response code="200">Transactions retrieved successfully</response>
        /// <response code="404">No system transactions found</response>
        [HttpGet(ApiRoutes.Transaction.GetAll)]
        public async Task<IActionResult> GetAllTransactions([FromQuery] PaginationNoFilterRequest request)
        {
            var query = _mapper.Map<GetTransactionsQuery>(request);

            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<string>.Fail("Transactions not found"));
            }
            var items = _mapper.Map<IEnumerable<TransactionResponse>>(result.Items);

            var response = new PaginationResponse<TransactionResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<TransactionResponse>>.Ok(response, "Get transactions successfully"));
        }
    }
}
