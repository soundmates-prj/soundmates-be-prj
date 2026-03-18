using AccountContentService.Api.Common;
using AccountContentService.Api.Contracts.Requests;
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
    [Authorize]
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public PaymentsController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [HttpPost("create")]
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

        [HttpGet("callback/vnpay")]
        public async Task<IActionResult> VNPayCallback(
    [FromQuery] string vnp_Amount,
    [FromQuery] string vnp_ResponseCode,
    [FromQuery] string vnp_TxnRef,
    [FromQuery] string vnp_OrderInfo
)
        {
            var data = Request.Query.ToDictionary(
                k => k.Key,
                v => v.Value.ToString()
            );

            var result = await _mediator.Send(new VNPayCallbackCommand
            {
                Data = data
            });

            return Ok(result);
        }

        [HttpPost("webhook/payos")]
        public IActionResult PayOSWebhook()
        {
            return Ok();
        }
    }
}
