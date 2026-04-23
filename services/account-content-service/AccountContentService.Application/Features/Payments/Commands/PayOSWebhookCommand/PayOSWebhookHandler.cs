using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using AccountContentService.Application.Interfaces.Services;

namespace AccountContentService.Application.Features.Payments.Commands.PayOSWebhookCommand;

public sealed class PayOSWebhookHandler : IRequestHandler<PayOSWebhookCommand, bool>
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IPaymentTransactionRepository _transactionRepo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly IPendingPayoutRepository _pendingPayoutRepo;
    private readonly ILiveSessionApiClient _liveSessionApiClient;
    private readonly ILogger<PayOSWebhookHandler> _logger;

    public PayOSWebhookHandler(
        IPaymentRepository paymentRepo,
        IPaymentTransactionRepository transactionRepo,
        ISubscriptionRepository subscriptionRepo,
        IPendingPayoutRepository pendingPayoutRepo,
        ILiveSessionApiClient liveSessionApiClient,
        ILogger<PayOSWebhookHandler> logger)
    {
        _paymentRepo = paymentRepo;
        _transactionRepo = transactionRepo;
        _subscriptionRepo = subscriptionRepo;
        _pendingPayoutRepo = pendingPayoutRepo;
        _liveSessionApiClient = liveSessionApiClient;
        _logger = logger;
    }

    public async Task<bool> Handle(PayOSWebhookCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing PayOS webhook for orderId {OrderId}, orderCode {OrderCode}, status {Status}",
            request.OrderId, request.OrderCode, request.Status);

        Payment? payment = null;

        if (request.OrderCode.HasValue)
        {
            payment = await _paymentRepo.GetByOrderCodeAsync(request.OrderCode.Value, cancellationToken);
        }

        // Fallback: try to find by OrderId (which now carries the payment GUID from "reference" field)
        if (payment == null && !string.IsNullOrWhiteSpace(request.OrderId)
            && Guid.TryParse(request.OrderId, out var paymentId))
        {
            payment = await _paymentRepo.GetByIdAsync(paymentId, cancellationToken);
        }

        if (payment == null)
        {
            _logger.LogWarning("Payment not found for PayOS orderId {OrderId} / orderCode {OrderCode}", request.OrderId, request.OrderCode);
            return false;
        }

        // Idempotency: skip if already processed
        if (payment.Status == PaymentStatus.Success.ToString())
        {
            _logger.LogInformation("Payment {PaymentId} already processed, skipping", payment.Id);
            return true;
        }

        var isSuccess = request.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase);

        // Create transaction record
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            PaymentProvider = PaymentProvider.PayOS.ToString(),
            PaymentMethod = PaymentMethod.QR.ToString(),
            Amount = payment.TotalAmount,
            PaymentAt = request.TransactionDateTime.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(request.TransactionDateTime.Value).UtcDateTime
                : DateTime.UtcNow,
            TransactionStatus = isSuccess ? TransactionStatus.Success.ToString() : TransactionStatus.Failed.ToString(),
            ResponsePayload = JsonSerializer.Serialize(request),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _transactionRepo.AddAsync(transaction);

        // Update payment status
        payment.Status = isSuccess ? PaymentStatus.Success.ToString() : PaymentStatus.Failed.ToString();
        payment.ExternalReference = request.PaymentLinkId;
        payment.UpdatedAt = DateTime.UtcNow;
        await _paymentRepo.UpdateAsync(payment);

        // Create subscription or process podcast if success
        if (isSuccess)
        {
            if (payment.TargetType.Equals("podcast", StringComparison.OrdinalIgnoreCase))
            {
                var podcast = await _liveSessionApiClient.GetPodcastAsync(payment.TargetId, cancellationToken);
                if (podcast != null)
                {
                    // Create pending payout
                    var pendingPayout = new PendingPayout
                    {
                        Id = Guid.NewGuid(),
                        PaymentId = payment.Id,
                        TargetUserId = podcast.CreatedBy,
                        Amount = podcast.Price, // Just the price, fee is kept by the system
                        Status = "pending",
                        ScheduledAt = DateTime.UtcNow.AddMinutes(5),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _pendingPayoutRepo.AddAsync(pendingPayout);
                    
                    _logger.LogInformation("Scheduled Payout for Podcast {PodcastId} to User {UserId} at {ScheduledAt}", payment.TargetId, podcast.CreatedBy, pendingPayout.ScheduledAt);

                    // Note: Actual access granting logic (e.g. adding to UserPurchasedPodcast)
                    // would be done via another API call or event publishing here if needed.
                }
            }
            else
            {
                // Expire any existing active subscription first (handles plan upgrade / renewal)
                var existingSub = await _subscriptionRepo.GetActiveByUserIdAsync(payment.UserId, cancellationToken);
                if (existingSub != null)
                {
                    existingSub.Status = SubscriptionStatus.Expired.ToString();
                    existingSub.EndDate = DateTime.UtcNow;
                    await _subscriptionRepo.UpdateAsync(existingSub);
                }

                var plan = await _subscriptionRepo.GetPlanByIdAsync(payment.TargetId, cancellationToken);
                if (plan != null)
                {
                    var subscription = new Subscription
                    {
                        Id = Guid.NewGuid(),
                        UserId = payment.UserId,
                        PlanId = payment.TargetId,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddMonths(1),
                        SubscribeAt = DateTime.UtcNow,
                        Status = SubscriptionStatus.Active.ToString(),
                    };
                    await _subscriptionRepo.AddAsync(subscription);
                }
            }
        }

        _logger.LogInformation("PayOS webhook processed: order {OrderId} -> {Status}",
            request.OrderId, isSuccess ? "SUCCESS" : "FAILED");

        return true;
    }
}
