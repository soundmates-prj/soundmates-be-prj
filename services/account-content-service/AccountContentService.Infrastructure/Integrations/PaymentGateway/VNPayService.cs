using AccountContentService.Application.Features.Payments.Commands.CreatePayment;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace AccountContentService.Infrastructure.NotificationService.PaymentGateway;

public class VNPayService : IPaymentProvider
{
    public string Name => "vnpay";

    private readonly VNPayConfig _config;

    public VNPayService(IOptions<VNPayConfig> config)
    {
        _config = config.Value;
    }

    public Task<string> CreatePaymentUrlAsync(Guid orderId, CreatePaymentCommand request)
    {
        var returnUrl = _config.ReturnUrl;

        var vnpay = new VNPayLibrary();

        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", _config.TmnCode);
        vnpay.AddRequestData("vnp_Amount", ((int)(request.TotalAmount * 100)).ToString());
        vnpay.AddRequestData("vnp_CreateDate", DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");

        vnpay.AddRequestData("vnp_IpAddr", string.IsNullOrEmpty(request.IpAddress) ? "127.0.0.1" : request.IpAddress);

        vnpay.AddRequestData("vnp_OrderInfo", $"{request.TargetType}_{request.TargetId}");
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_Locale", "vn");

        vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
        vnpay.AddRequestData("vnp_TxnRef", orderId.ToString());

        var baseUrl = _config.BaseUrl;
        var url = vnpay.CreateRequestUrl(baseUrl, _config.HashSecret);

        return Task.FromResult(url);
    }

    public Task<bool> ExecutePayoutAsync(Guid payoutId, decimal amount, string bankId, string accountNumber, string accountName, string description)
    {
        throw new NotSupportedException("VNPay integration does not support payouts yet.");
    }
}
