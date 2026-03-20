using AccountContentService.Api.Common;
using AccountContentService.Api.Constants;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.Features.Payments.Commands;
using AccountContentService.Application.Features.Payments.Commands.CallbackCommand;
using AccountContentService.Application.Features.Payments.Commands.CreatePayment;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AccountContentService.Api.Controllers
{
    /// <summary>
    /// Handles all payment-related operations including payment creation,
    /// external gateway callbacks (VNPay), and webhook integrations (PayOS).
    /// </summary>
    /// <remarks>
    /// This controller acts as an entry point for payment workflows in the system.
    /// It integrates with external payment providers and coordinates payment processing via CQRS (MediatR).
    /// </remarks>
    [Authorize]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentsController"/>.
        /// </summary>
        /// <param name="mediator">MediatR instance used to dispatch commands.</param>
        /// <param name="mapper">AutoMapper instance for object mapping.</param>
        public PaymentsController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        /// <summary>
        /// Creates a new payment request and returns a payment URL.
        /// </summary>
        /// <param name="request">Payment request data including subscription or order details.</param>
        /// <returns>
        /// Returns a payment URL that the client should redirect the user to for completing payment.
        /// </returns>
        /// <remarks>
        /// Flow:
        /// 1. Extract UserId from JWT token.
        /// 2. Extract client IP address.
        /// 3. Map request to CreatePaymentCommand.
        /// 4. Send command to application layer.
        /// 5. Return payment gateway URL (VNPay/PayOS).
        /// </remarks>
        /// <response code="200">Payment URL generated successfully.</response>
        /// <response code="400">Invalid request data.</response>
        [HttpPost(ApiRoutes.Payments.Create)]
        public async Task<IActionResult> CreatePayment(PaymentRequest request)
        {
            var userId = UserContext.GetUserId(HttpContext);
            var ipAddress = RequestContext.GetIpAddress(HttpContext);

            var command = _mapper.Map<CreatePaymentCommand>(request);
            command.UserId = userId;
            command.IpAddress = ipAddress;

            var url = await _mediator.Send(command);

            return Ok(new { paymentUrl = url });
        }

        /// <summary>
        /// Handles VNPay callback after user completes payment.
        /// </summary>
        /// <remarks>
        /// VNPay will redirect the user to this endpoint with query parameters.
        /// The system will:
        /// 1. Extract query parameters from VNPay.
        /// 2. Validate secure hash/signature.
        /// 3. Add a transaction.
        /// 4. Update payment & subscription status.
        /// 5. Return processing result.
        /// </remarks>
        /// <response code="200">Callback processed successfully.</response>
        /// <response code="400">Invalid or tampered VNPay data.</response>
        [HttpGet(ApiRoutes.Payments.VNPayCallBack)]
        public async Task<IActionResult> VNPayCallback()
        {
            var data = Request.Query.ToDictionary(
                k => k.Key,
                v => v.Value.ToString()
            );

            var result = await _mediator.Send(new VNPayCallbackCommand
            {
                Data = data
            });

            var response = _mapper.Map<TransactionResponse>(result);
            return Ok(ApiResponse<TransactionResponse>.Ok(response, "Create post successfully"));
        }

        /// <summary>
        /// Receives webhook notifications from PayOS.
        /// </summary>
        /// <returns>Returns 200 OK to acknowledge webhook receipt.</returns>
        /// <remarks>
        /// This endpoint is intended for PayOS server-to-server communication.
        /// It should:
        /// - Validate webhook signature (future implementation)
        /// - Update payment status asynchronously
        /// 
        /// Currently implemented as a placeholder.
        /// </remarks>
        /// <response code="200">Webhook received successfully.</response>
        [HttpPost(ApiRoutes.Payments.PayOsWebhook)]
        public IActionResult PayOSWebhook()
        {
            return Ok();
        }
    }
}