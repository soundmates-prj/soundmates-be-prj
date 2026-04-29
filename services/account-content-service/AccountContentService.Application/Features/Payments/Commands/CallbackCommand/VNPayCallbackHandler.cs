using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Application.Interfaces;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using AutoMapper;
using MediatR;
using System.Text.Json;

namespace AccountContentService.Application.Features.Payments.Commands.CallbackCommand
{
    public class VNPayCallbackHandler : IRequestHandler<VNPayCallbackCommand, TransactionDto>
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IPaymentTransactionRepository _transactionRepo;
        private readonly ISubscriptionRepository _subscriptionRepo;
        private readonly IPaymentService _paymentService;
        private readonly IPendingPayoutRepository _pendingPayoutRepo;
        private readonly ILiveSessionApiClient _liveSessionApiClient;
        private readonly IAuthApiClient _authApiClient;
        private readonly INotificationRepository _notificationRepo;
        private readonly INotificationPusher _notificationPusher;
        private readonly IMapper _mapper;   

        public VNPayCallbackHandler(
            IPaymentRepository paymentRepo,
            IPaymentTransactionRepository transactionRepo,
            ISubscriptionRepository subscriptionRepo,
            IPaymentService paymentService,
            IPendingPayoutRepository pendingPayoutRepo,
            ILiveSessionApiClient liveSessionApiClient,
            IAuthApiClient authApiClient,
            INotificationRepository notificationRepo,
            INotificationPusher notificationPusher,
            IMapper mapper)
        {
            _paymentRepo = paymentRepo;
            _transactionRepo = transactionRepo;
            _subscriptionRepo = subscriptionRepo;
            _paymentService = paymentService;
            _pendingPayoutRepo = pendingPayoutRepo;
            _liveSessionApiClient = liveSessionApiClient;
            _authApiClient = authApiClient;
            _notificationRepo = notificationRepo;
            _notificationPusher = notificationPusher;
            _mapper = mapper;
        }

        public async Task<TransactionDto> Handle(VNPayCallbackCommand request, CancellationToken cancellationToken)
        {
            var data = request.Data;

            // 🔥 1. Verify signature (KHÔNG dùng _config nữa)
            var isValid = _paymentService.VNPayVerifySignature(data);

            if (!isValid)
                throw new Exception("Invalid signature");

            // 🔥 2. Parse dữ liệu
            var orderId = data["vnp_TxnRef"];
            var responseCode = data["vnp_ResponseCode"];
            var transactionNo = data["vnp_TransactionNo"];

            if (!Guid.TryParse(orderId, out var paymentId))
                throw new Exception("Invalid paymentId");

            // 🔥 3. Find Payment
            var payment = await _paymentRepo.GetByIdAsync(paymentId, cancellationToken);
            if (payment == null)
                throw new Exception("Payment not found");

            // 🔥 4. Idempotency Check
            // Nếu Payment đã có trạng thái 'Success', nghĩa là giao dịch này đã được xử lý xong từ trước.
            // Thay vì trả về lỗi (khiến Frontend bị crash), chúng ta tìm lại giao dịch cũ đã thành công để trả về cho người dùng.
            if (payment.Status == PaymentStatus.Success.ToString())
            {
                var existingTransaction = await _transactionRepo.GetByPaymentIdAsync(payment.Id, cancellationToken);
                if (existingTransaction != null)
                {
                    var existingResponse = _mapper.Map<TransactionDto>(existingTransaction);
                    existingResponse.TargetType = payment.TargetType;
                    existingResponse.TargetId = payment.TargetId;
                    return existingResponse;
                }
                throw new Exception("Payment already processed but transaction not found");
            }

            var isSuccess = responseCode == "00";

            // 🔥 5. Create Transaction
            var transaction = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                PaymentProvider = PaymentProvider.VNPay.ToString(),
                PaymentMethod = PaymentMethod.QR.ToString(),
                Amount = payment.TotalAmount,
                PaymentAt = DateTime.UtcNow,
                TransactionStatus = isSuccess ? TransactionStatus.Success.ToString() : TransactionStatus.Failed.ToString(),
                ResponsePayload = JsonSerializer.Serialize(data),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _transactionRepo.AddAsync(transaction);

            // 🔥 6. Update Payment
            payment.Status = isSuccess ? PaymentStatus.Success.ToString() : PaymentStatus.Failed.ToString();
            payment.ExternalReference = transactionNo;
            payment.UpdatedAt = DateTime.UtcNow;

            await _paymentRepo.UpdateAsync(payment);

            // 🔥 7. Process TargetType (Podcast Payout or Subscription)
            if (isSuccess)
            {
                if (payment.TargetType.Equals("podcast", StringComparison.OrdinalIgnoreCase))
                {
                    var podcast = await _liveSessionApiClient.GetPodcastAsync(payment.TargetId, cancellationToken);
                    if (podcast != null)
                    {
                        var bankAccount = await _authApiClient.GetUserBankAccountAsync(podcast.CreatedBy, cancellationToken);

                        var pendingPayout = new PendingPayout
                        {
                            Id = Guid.NewGuid(),
                            PaymentId = payment.Id,
                            TargetUserId = podcast.CreatedBy,
                            Amount = podcast.Price * 0.8m, // 🔥 Platform keeps 20%
                            BankId = bankAccount?.BankId,
                            AccountNumber = bankAccount?.AccountNumber,
                            AccountName = bankAccount?.AccountName,
                            Status = bankAccount != null ? "pending" : "failed_no_bank",
                            ErrorMessage = bankAccount == null ? "User has no bank account configured." : null,
                            ScheduledAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        await _pendingPayoutRepo.AddAsync(pendingPayout);
                        
                        // 🔥 7.1 Grant Podcast Access to Buyer
                        await _liveSessionApiClient.GrantPodcastAccessAsync(payment.TargetId, payment.UserId, podcast.Price, cancellationToken);

                        // 🔥 7.2 Send notification to Podcast Owner
                        var notification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            UserId = podcast.CreatedBy,
                            Type = "podcast_purchased",
                            ReferenceId = payment.TargetId,
                            Message = $"Chúc mừng! Có người vừa mua podcast \"{podcast.Title}\" của bạn. Bạn nhận được {podcast.Price * 0.8m:N0}đ.",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _notificationRepo.AddAsync(notification, cancellationToken);
                        await _notificationPusher.PushToUserAsync(podcast.CreatedBy, notification, cancellationToken);
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

            var respose = _mapper.Map<TransactionDto>(transaction);
            respose.TargetType = payment.TargetType;
            respose.TargetId = payment.TargetId;
            return respose;
        }
    }
}