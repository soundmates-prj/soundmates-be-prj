using AccountContentService.Api.Common;
using AccountContentService.Api.Constants;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.Features.Payments.Commands;
using AccountContentService.Application.Features.Payments.Commands.CallbackCommand;
using AccountContentService.Application.Features.Payments.Commands.CreatePayment;
using AccountContentService.Application.Features.Payments.Commands.PayOSWebhookCommand;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

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
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentsController"/>.
        /// </summary>
        /// <param name="mediator">MediatR instance used to dispatch commands.</param>
        /// <param name="mapper">AutoMapper instance for object mapping.</param>
        /// <param name="configuration">App configuration for reading FrontendUrl.</param>
        public PaymentsController(IMediator mediator, IMapper mapper, IConfiguration configuration)
        {
            _mediator = mediator;
            _mapper = mapper;
            _configuration = configuration;
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

            // Only resolve default if not provided by frontend.
            // If request.ReturnUrl stays null, the provider's own default from config will be used.
            command.ReturnUrl ??= string.IsNullOrWhiteSpace(request.ReturnUrl) 
                ? ResolveDefaultReturnUrl(request.Method) 
                : request.ReturnUrl;

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
        /// 5. Redirect user to frontend result page.
        /// </remarks>
        /// <response code="302">Redirect to frontend payment result page.</response>
        /// <response code="400">Invalid or tampered VNPay data.</response>
        [AllowAnonymous]
        [HttpGet(ApiRoutes.Payments.VNPayCallBack)]
        public async Task<IActionResult> VNPayCallback()
        {
            var data = Request.Query.ToDictionary(
                k => k.Key,
                v => v.Value.ToString()
            );

            var frontendUrl = _configuration["AppSettings:FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
            var returnPage = $"{frontendUrl}/payment/result";

            var isJsonRequest = Request.Headers["Accept"].ToString().Contains("application/json");

            try
            {
                var result = await _mediator.Send(new VNPayCallbackCommand { Data = data });

                if (isJsonRequest)
                {
                    return Ok(ApiResponse<object>.Ok(new
                    {
                        status = "success",
                        transactionId = result.Id,
                        paymentId = result.PaymentId,
                        amount = result.Amount,
                        provider = result.PaymentProvider,
                        vnp_TransactionNo = data.GetValueOrDefault("vnp_TransactionNo", ""),
                        transactionStatus = result.TransactionStatus
                    }, "Payment processed successfully"));
                }

                var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
                query["status"] = result.TransactionStatus.ToLower();
                query["transactionId"] = result.Id.ToString();
                query["paymentId"] = result.PaymentId.ToString();
                query["amount"] = result.Amount.ToString();
                query["provider"] = "vnpay";
                query["vnp_ResponseCode"] = data.GetValueOrDefault("vnp_ResponseCode", "");
                query["vnp_TransactionNo"] = data.GetValueOrDefault("vnp_TransactionNo", "");

                return Redirect($"{returnPage}?{query}");
            }
            catch (Exception ex)
            {
                if (isJsonRequest)
                {
                    return BadRequest(ApiResponse<string>.Fail(ex.Message));
                }

                var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
                query["status"] = "failed";
                query["message"] = ex.Message;
                query["provider"] = "vnpay";
                query["vnp_ResponseCode"] = data.GetValueOrDefault("vnp_ResponseCode", "");

                return Redirect($"{returnPage}?{query}");
            }
        }

        /// <summary>
        /// Handles PayOS return URL (GET) after user completes or cancels payment on PayOS.
        /// Redirects user to frontend result page with payment result.
        /// </summary>
        /// <remarks>
        /// This is the GET returnUrl that PayOS redirects to after payment.
        /// It processes the payment result and redirects to the frontend.
        /// </remarks>
        /// <response code="302">Redirect to frontend payment result page.</response>
        [AllowAnonymous]
        [HttpGet(ApiRoutes.Payments.PayOsReturn)]
        public async Task<IActionResult> PayOSReturn()
        {
            var frontendUrl = _configuration["AppSettings:FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
            var returnPage = $"{frontendUrl}/payment/result";

            var code = Request.Query["code"].ToString();
            var id = Request.Query["id"].ToString();
            var status = Request.Query["status"].ToString();
            var orderId = Request.Query["orderId"].ToString();
            var paymentLinkId = Request.Query["paymentLinkId"].ToString();
            var signature = Request.Query["signature"].ToString();

            // PayOS success: code = "00" or status = "PAID"
            var isSuccess = status.Equals("PAID", StringComparison.OrdinalIgnoreCase)
                || code == "00";

            var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
            query["provider"] = "payos";
            query["status"] = isSuccess ? "success" : "failed";
            query["code"] = code;
            query["payos_TransactionNo"] = paymentLinkId;
            query["paymentId"] = orderId;
            query["message"] = GetPayOSMessage(code, status);

            return Redirect($"{returnPage}?{query}");
        }

        /// <summary>
        /// Receives webhook notifications from PayOS (server-to-server).
        /// </summary>
        /// <remarks>
        /// PayOS calls this endpoint to notify payment status changes.
        /// It should validate the webhook signature and update payment status.
        /// </remarks>
        /// <response code="200">Webhook received successfully.</response>
        [AllowAnonymous]
        [HttpPost(ApiRoutes.Payments.PayOsWebhook)]
        public async Task<IActionResult> PayOSWebhook(
            [FromBody] PayOSWebhookRequest webhookData,
            CancellationToken cancellationToken)
        {
            if (webhookData == null)
            {
                return BadRequest(ApiResponse<string>.Fail("Invalid webhook payload"));
            }

            try
            {
                // TODO: Validate PayOS webhook signature using checksum
                // var isValid = ValidatePayOSWebhook(webhookData);
                // if (!isValid) return Unauthorized();

                var source = webhookData.data ?? webhookData;
                var status = !string.IsNullOrWhiteSpace(source.status)
                    ? source.status
                    : ((source.code ?? webhookData.code) == "00" ? "PAID" : "FAILED");

                // PayOS sends orderCode inside data.data (not data.orderCode).
                // Reference is the paymentId (GUID) that we sent as orderId in CreatePaymentCommand.
                var orderCode = ExtractOrderCode(source);
                var paymentId = !string.IsNullOrWhiteSpace(source.reference)
                    ? source.reference
                    : source.orderId ?? string.Empty;

                var processed = await _mediator.Send(new PayOSWebhookCommand
                {
                    OrderId = paymentId,
                    OrderCode = orderCode,
                    PaymentLinkId = source.paymentLinkId,
                    Amount = source.amount,
                    Status = status,
                    TransactionDateTime = ParseTransactionDateTime(source.transactionDateTime),
                    Signature = webhookData.signature ?? source.signature ?? string.Empty
                }, cancellationToken);

                if (!processed)
                {
                    // To satisfy PayOS verification test which sends dummy data, return Ok even if not matched.
                    return Ok(ApiResponse<string>.Ok("OK", 
                        $"Webhook received but payment not matched (likely a test). orderCode={source.orderCode}, orderId={source.orderId}"));
                }

                return Ok(ApiResponse<string>.Ok("OK", "Webhook received"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
        }

        private static long? ExtractOrderCode(PayOSWebhookRequest source)
        {
            // PayOS v2 sends orderCode as a top-level number field (e.g. 1775592558831).
            // In the webhook payload: data.orderCode = 1775592558831
            if (source.orderCode.HasValue && source.orderCode.Value != 0)
                return source.orderCode.Value;

            // Fallback: try to find a Unix-millisecond timestamp in the description.
            // Description format: "plantype_paymentguid" (e.g. "premium-019d696e...")
            // The timestamp part is set by DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            // which is always > 1 trillion.
            if (!string.IsNullOrWhiteSpace(source.desc))
            {
                foreach (var part in source.desc.Split('_', '-'))
                {
                    if (long.TryParse(part, out var parsed) && parsed > 1_000_000_000_000)
                        return parsed;
                }
            }

            return null;
        }

        private static string GetPayOSMessage(string code, string status)
        {
            if (status.Equals("PAID", StringComparison.OrdinalIgnoreCase) || code == "00")
                return "Giao dịch thành công";
            if (code == "-01")
                return "Giao dịch bị hủy bởi người dùng";
            if (code == "-02")
                return "Giao dịch thất bại";
            if (code == "-03")
                return "Giao dịch đang xử lý";
            if (code == "-04")
                return "Giao dịch hết hạn";
            return $"Thanh toán PayOS thất bại (code: {code}, status: {status})";
        }

        /// <summary>
        /// Resolves the default return URL used when frontend doesn't provide one.
        /// Must point to the backend callback endpoint so transaction & subscription
        /// are created server-side before redirecting to the frontend result page.
        /// </summary>
        private string ResolveDefaultReturnUrl(string method)
        {
            var apiBaseUrl = _configuration["AppSettings:BaseUrl"]?.TrimEnd('/');
            
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                // Fallback to current request's host (protocol://domain:port)
                // This is ideal for development/internal networks where BASE_URL is not set.
                apiBaseUrl = $"{Request.Scheme}://{Request.Host}";
            }

            var path = method.ToLower() switch
            {
                "vnpay" => ApiRoutes.Payments.VNPayCallBack,
                "payos" => ApiRoutes.Payments.PayOsReturn,
                _ => ApiRoutes.Payments.VNPayCallBack // Default fallback
            };

            return $"{apiBaseUrl}/{path.TrimStart('/')}";
        }

        private static long? ParseTransactionDateTime(JsonElement? raw)
        {
            if (!raw.HasValue)
                return null;

            var value = raw.Value;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var numeric))
                return numeric;

            if (value.ValueKind != JsonValueKind.String)
                return null;

            var text = value.GetString();
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (long.TryParse(text, out var milliseconds))
                return milliseconds;

            if (DateTime.TryParseExact(
                    text,
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var parsed))
            {
                return new DateTimeOffset(parsed).ToUnixTimeMilliseconds();
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
            {
                return new DateTimeOffset(parsed).ToUnixTimeMilliseconds();
            }

            return null;
        }
    }

    public class PayOSWebhookRequest
    {
        public string? code { get; set; }
        public string? desc { get; set; }
        public bool? success { get; set; }

        public string orderId { get; set; } = string.Empty;
        public long? orderCode { get; set; }
        public string? reference { get; set; }
        public string paymentLinkId { get; set; } = string.Empty;
        public int amount { get; set; }
        public string status { get; set; } = string.Empty;
        public JsonElement? transactionDateTime { get; set; }
        public string? signature { get; set; }
        public string? cancelReason { get; set; }

        public PayOSWebhookRequest? data { get; set; }
    }
}