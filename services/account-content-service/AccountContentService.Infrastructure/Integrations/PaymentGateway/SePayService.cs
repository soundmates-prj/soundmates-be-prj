using AccountContentService.Application.Features.Payments.Commands.CreatePayment;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AccountContentService.Infrastructure.Integrations.PaymentGateway;

public class SePayService : IPaymentProvider
{
    public string Name => "sepay";

    private readonly SePayConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SePayService> _logger;

    public SePayService(
        IOptions<SePayConfig> config,
        ILogger<SePayService> logger)
    {
        _config = config.Value;
        _logger = logger;

        var baseUrl = _config.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "https://my.sepay.vn";

        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl, UriKind.Absolute) };

        if (!string.IsNullOrWhiteSpace(_config.ClientSecret))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ClientSecret}");
        }
    }

    public Task<string> CreatePaymentUrlAsync(Guid orderId, CreatePaymentCommand request)
    {
        throw new NotSupportedException("SePay is only configured for Payouts in this implementation.");
    }

    public async Task<bool> ExecutePayoutAsync(Guid payoutId, decimal amount, string bankId, string accountNumber, string accountName, string description)
    {
        var payoutAmount = (int)amount;
        var reference = payoutId.ToString("N");

        // SePay Transfer API requires Bank Code/BIN, Account Number, Amount, Memo
        var payload = new
        {
            bank_name = bankId, // SePay API accepts Bank BIN or Bank short name
            account_number = accountNumber,
            account_name = accountName,
            amount = payoutAmount,
            memo = description.Length > 20 ? description[..20] : description
        };

        _logger.LogInformation("Creating SePay Payout for {PayoutId}, amount {Amount} to {AccountName} ({BankId})", payoutId, amount, accountName, bankId);

        try
        {
            // The SePay Transfer API endpoint
            var response = await _httpClient.PostAsJsonAsync("/userapi/bank-transfers", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("SePay Payout API error: {Status} - {Body}", response.StatusCode, errorContent);
                return false;
            }

            // SePay typically returns standard JSON response
            var result = await response.Content.ReadFromJsonAsync<SePayTransferResponse>();
            if (result is null)
            {
                _logger.LogError("SePay returned empty response for payout");
                return false;
            }

            if (!result.success && result.status != 200)
            {
                _logger.LogError("SePay Payout failed: {Message}", result.message ?? result.error);
                return false;
            }

            _logger.LogInformation("SePay Payout request submitted successfully for {PayoutId}", payoutId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing SePay Payout {PayoutId}", payoutId);
            return false;
        }
    }
}

internal class SePayTransferResponse
{
    [JsonPropertyName("success")]
    public bool success { get; set; }

    [JsonPropertyName("status")]
    public int status { get; set; }

    [JsonPropertyName("message")]
    public string? message { get; set; }
    
    [JsonPropertyName("error")]
    public string? error { get; set; }
}
