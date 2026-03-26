using AccountContentService.Application.Features.Payments.Commands.CreatePayment;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AccountContentService.Infrastructure.NotificationService.PaymentGateway;

public class PayOSService : IPaymentProvider
{
    public string Name => "payos";

    private readonly PayOSConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PayOSService> _logger;
    private readonly HttpClient _httpClient;

    public PayOSService(
        IOptions<PayOSConfig> config,
        IHttpClientFactory httpClientFactory,
        ILogger<PayOSService> logger)
    {
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _httpClient = _httpClientFactory.CreateClient("PayOS");
    }

    public async Task<string> CreatePaymentUrlAsync(Guid orderId, CreatePaymentCommand request)
    {
        var amount = (int)request.TotalAmount; // PayOS requires integer VND
        var description = $"{request.TargetType}_{request.TargetId}";
        var orderIdStr = orderId.ToString();

        // Use dynamic ReturnUrl from frontend if provided, otherwise fallback to config
        var returnUrl = ResolveReturnUrl(request.ReturnUrl);
        var cancelUrl = returnUrl; // Cancel redirects to same page as return

        // Build signature data (alphabetically sorted by key)
        var signatureData = $"amount={amount}&cancelUrl={Uri.EscapeDataString(cancelUrl)}&description={Uri.EscapeDataString(description)}&orderId={orderIdStr}&returnUrl={Uri.EscapeDataString(returnUrl)}";
        var signature = ComputeHmacSha256(signatureData, _config.ChecksumKey);

        var payload = new PayOSCreatePaymentRequest
        {
            clientId = _config.ClientId,
            apiKey = _config.ApiKey,
            amount = amount,
            cancellationToken = "",
            description = description,
            orderId = orderIdStr,
            returnUrl = returnUrl,
            cancelUrl = cancelUrl,
            signature = signature,
            buyerEmail = "",
            buyerName = "",
            buyerPhone = ""
        };

        _logger.LogInformation("Creating PayOS payment link for order {OrderId}, amount {Amount}", orderId, amount);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_config.BaseUrl}/v2/payment-requests",
                payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("PayOS API error: {Status} - {Body}", response.StatusCode, errorContent);
                throw new Exception($"PayOS API error: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PayOSCreatePaymentResponse>();

            if (result?.data?.checkoutUrl == null)
            {
                _logger.LogError("PayOS returned no checkoutUrl. Response: {Response}", JsonSerializer.Serialize(result));
                throw new Exception("PayOS did not return a checkout URL");
            }

            _logger.LogInformation("PayOS payment link created: {CheckoutUrl}", result.data.checkoutUrl);
            return result.data.checkoutUrl;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling PayOS API");
            throw new Exception("Failed to connect to PayOS", ex);
        }
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Resolves the return URL: use frontend-provided URL if available,
    /// otherwise fall back to the configured server-side ReturnUrl.
    /// </summary>
    private string ResolveReturnUrl(string? requestReturnUrl)
    {
        if (!string.IsNullOrWhiteSpace(requestReturnUrl))
            return requestReturnUrl.TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(_config.ReturnUrl))
            return _config.ReturnUrl.TrimEnd('/');

        throw new InvalidOperationException(
            "No ReturnUrl provided by frontend and PayOS:ReturnUrl is not configured. " +
            "Set PayOS__ReturnUrl in .env or pass ReturnUrl in the payment request.");
    }
}

// PayOS Request/Response models

internal class PayOSCreatePaymentRequest
{
    [JsonPropertyName("clientId")]
    public string clientId { get; set; } = string.Empty;

    [JsonPropertyName("apiKey")]
    public string apiKey { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public int amount { get; set; }

    [JsonPropertyName("cancellationToken")]
    public string cancellationToken { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string description { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public string orderId { get; set; } = string.Empty;

    [JsonPropertyName("returnUrl")]
    public string returnUrl { get; set; } = string.Empty;

    [JsonPropertyName("cancelUrl")]
    public string cancelUrl { get; set; } = string.Empty;

    [JsonPropertyName("signature")]
    public string signature { get; set; } = string.Empty;

    [JsonPropertyName("buyerEmail")]
    public string buyerEmail { get; set; } = string.Empty;

    [JsonPropertyName("buyerName")]
    public string buyerName { get; set; } = string.Empty;

    [JsonPropertyName("buyerPhone")]
    public string buyerPhone { get; set; } = string.Empty;
}

internal class PayOSCreatePaymentResponse
{
    [JsonPropertyName("success")]
    public bool success { get; set; }

    [JsonPropertyName("message")]
    public string? message { get; set; }

    [JsonPropertyName("data")]
    public PayOSPaymentData? data { get; set; }
}

internal class PayOSPaymentData
{
    [JsonPropertyName("checkoutUrl")]
    public string? checkoutUrl { get; set; }

    [JsonPropertyName("orderId")]
    public string? orderId { get; set; }

    [JsonPropertyName("paymentLinkId")]
    public string? paymentLinkId { get; set; }

    [JsonPropertyName("status")]
    public string? status { get; set; }
}
