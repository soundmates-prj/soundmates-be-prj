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
using System.Linq;
using System.Threading.Tasks;

namespace AccountContentService.Infrastructure.NotificationService.PaymentGateway;

public class PayOSService : IPaymentProvider
{
    public string Name => "payos";

    private readonly PayOSConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayOSService> _logger;

    public PayOSService(
        IOptions<PayOSConfig> config,
        ILogger<PayOSService> logger)
    {
        _config = config.Value;

        _logger = logger;
        var baseUrl = ResolveBaseUrl();
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl, UriKind.Absolute) };

        // PayOS v2 API — x-client-id = Client ID (Partner Code), x-api-key = API Key.
        _httpClient.DefaultRequestHeaders.Add("x-client-id", _config.ClientId);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _config.ApiKey);
    }

    public async Task<string> CreatePaymentUrlAsync(Guid orderId, CreatePaymentCommand request)
    {
        var amount = (int)request.TotalAmount; // PayOS requires integer VND

        // PayOS orderCode must be Int32 (max 2,147,483,647). Cap to int range.
        int orderCode;
        if (request.OrderCode.HasValue)
        {
            var raw = request.OrderCode.Value;
            // Ensure it fits in Int32 — take absolute value then mod
            orderCode = (int)Math.Abs(raw % int.MaxValue);
        }
        else
        {
            // Use Unix seconds (fits comfortably in Int32 for many years)
            orderCode = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        // PayOS limits description to 9 chars for non-linked bank accounts.
        // Build a short, URL-safe identifier: planName_first8charsGUID
        var fullDescription = request.Description ?? $"payment_{orderId:N}";
        var shortDesc = fullDescription.Length > 9 ? fullDescription[..9] : fullDescription;

        // Prefer server-side ReturnUrl from config, fallback to request value only if config is missing.
        var returnUrl = ResolveReturnUrl(request.ReturnUrl);
        var cancelUrl = returnUrl; // Cancel redirects to same page as return

        var signature = GenerateSignature(orderCode, amount, shortDesc, returnUrl, cancelUrl);

        // Set link expiry to 15 minutes from now (Unix seconds)
        var expiredAt = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();

        var payload = new PayOSCreatePaymentRequest
        {
            amount = amount,
            description = shortDesc,
            orderCode = orderCode,
            returnUrl = returnUrl,
            cancelUrl = cancelUrl,
            signature = signature,
            expiredAt = expiredAt,
            items = new List<PayOSItemRequest>
            {
                new()
                {
                    name = "subscription",
                    quantity = 1,
                    price = amount
                }
            },
            buyerEmail = "",
            buyerName = "",
            buyerPhone = ""
        };

        _logger.LogInformation("Creating PayOS payment link for order {OrderId}, amount {Amount}", orderId, amount);

        // Full debug: log exact values being passed into signature
        _logger.LogDebug("PayOS shortDesc: {Desc}", shortDesc);
        _logger.LogDebug("PayOS returnUrl raw: {Url}", returnUrl);
        _logger.LogDebug("PayOS cancelUrl raw: {Url}", cancelUrl);

        _logger.LogWarning("=== PAYLOAD SENT === {Payload}", JsonSerializer.Serialize(payload));

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "v2/payment-requests",
                payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "PayOS API error: {Status} - {Body}",
                    response.StatusCode, errorContent);
                throw new Exception($"PayOS API error: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PayOSCreatePaymentResponse>();

            if (result is null)
            {
                throw new Exception("PayOS returned empty response");
            }

            // PayOS returns code="00" for success. Any other code (including null/empty)
            // means failure. Also check result.success false and no checkoutUrl.
            var hasErrorCode = !string.IsNullOrWhiteSpace(result.code) && result.code != "00";
            var hasFailedSuccessFlag = string.IsNullOrWhiteSpace(result.code) && !result.success;

            if (hasErrorCode || hasFailedSuccessFlag)
            {
                var providerMessage = !string.IsNullOrWhiteSpace(result.desc)
                    ? result.desc
                    : result.message;

                throw new Exception(string.IsNullOrWhiteSpace(providerMessage)
                    ? $"PayOS rejected payment request (code: {result.code ?? "(null)"})"
                    : $"PayOS rejected payment request: {providerMessage} (code: {result.code})");
            }

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

    private string GenerateSignature(int orderCode, int amount, string description, string returnUrl, string cancelUrl)
    {
        // PayOS signature: raw values (no URL encoding), keys sorted alphabetically.
        var rawData =
            $"amount={amount}" +
            $"&cancelUrl={cancelUrl}" +
            $"&description={description}" +
            $"&orderCode={orderCode}" +
            $"&returnUrl={returnUrl}";

        _logger.LogInformation("PayOS SIGN RAW: {RawData}", rawData);
        _logger.LogInformation("PayOS checksumKey (len={Len}): {Key}", _config.ChecksumKey.Length, _config.ChecksumKey);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.ChecksumKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var signature = BitConverter.ToString(hash).Replace("-", "").ToLower();

        _logger.LogInformation("PayOS signature: {Sig}", signature);
        return signature;
    }

    /// <summary>
    /// Resolves the return URL with server config priority.
    /// Always prefer PayOS ReturnUrl from environment/configuration,
    /// then fallback to frontend-provided returnUrl when config is missing.
    /// </summary>
    private string ResolveReturnUrl(string? requestReturnUrl)
    {
        if (TryGetValidAbsoluteUrl(_config.ReturnUrl, out var configuredUrl))
            return configuredUrl;

        if (TryGetValidAbsoluteUrl(requestReturnUrl, out var requestUrl))
            return requestUrl;

        throw new InvalidOperationException(
            "No ReturnUrl provided by frontend and PayOS:ReturnUrl is not configured. " +
            "Set PayOS__ReturnUrl in .env or pass ReturnUrl in the payment request.");
    }

    private string ResolveBaseUrl()
    {
        var candidate = !IsPlaceholderOrEmpty(_config.BaseUrl)
            ? _config.BaseUrl
            : (!IsPlaceholderOrEmpty(_config.SandboxBaseUrl)
                ? _config.SandboxBaseUrl
                : "https://api-merchant.payos.vn");

        candidate = candidate.Trim().TrimEnd('/');

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                $"Invalid PayOS base URL: '{candidate}'. Configure PayOS__BaseUrl or PAY_OS_BASE_URL with a valid absolute URL.");
        }

        return candidate;
    }

    private static bool IsPlaceholderOrEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var trimmed = value.Trim();
        return trimmed.StartsWith("${") && trimmed.EndsWith("}");
    }

    private static bool TryGetValidAbsoluteUrl(string? value, out string url)
    {
        url = string.Empty;

        if (IsPlaceholderOrEmpty(value))
            return false;

        var trimmed = value!.Trim().TrimEnd('/');
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out _))
            return false;

        url = trimmed;
        return true;
    }

    private static string BuildDescription(string? preferredDescription, string? targetType, Guid targetId)
    {
        var source = !string.IsNullOrWhiteSpace(preferredDescription)
            ? preferredDescription
            : targetType;

        var type = string.IsNullOrWhiteSpace(source)
            ? "payment"
            : new string(source.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        if (string.IsNullOrWhiteSpace(type))
            type = "payment";

        if (type.Length > 12)
            type = type[..12];

        var shortId = targetId.ToString("N")[..8];
        var description = $"{type}_{shortId}";

        // Keep payload compatible with providers that enforce 9-char description.
        return description.Length > 9 ? description[..9] : description;
    }
}

// PayOS Request/Response models

internal class PayOSCreatePaymentRequest
{
    [JsonPropertyName("amount")]
    public int amount { get; set; }

    [JsonPropertyName("description")]
    public string description { get; set; } = string.Empty;

    [JsonPropertyName("orderCode")]
    public int orderCode { get; set; }

    [JsonPropertyName("items")]
    public List<PayOSItemRequest> items { get; set; } = new();

    [JsonPropertyName("returnUrl")]
    public string returnUrl { get; set; } = string.Empty;

    [JsonPropertyName("cancelUrl")]
    public string cancelUrl { get; set; } = string.Empty;

    [JsonPropertyName("signature")]
    public string signature { get; set; } = string.Empty;

    [JsonPropertyName("expiredAt")]
    public long expiredAt { get; set; }

    [JsonPropertyName("buyerEmail")]
    public string buyerEmail { get; set; } = string.Empty;

    [JsonPropertyName("buyerName")]
    public string buyerName { get; set; } = string.Empty;

    [JsonPropertyName("buyerPhone")]
    public string buyerPhone { get; set; } = string.Empty;
}

internal class PayOSItemRequest
{
    [JsonPropertyName("name")]
    public string name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int quantity { get; set; }

    [JsonPropertyName("price")]
    public int price { get; set; }
}

internal class PayOSCreatePaymentResponse
{
    [JsonPropertyName("code")]
    public string? code { get; set; }

    [JsonPropertyName("desc")]
    public string? desc { get; set; }

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
