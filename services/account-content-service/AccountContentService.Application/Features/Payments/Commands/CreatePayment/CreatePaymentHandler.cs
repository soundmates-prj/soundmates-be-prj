using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using MediatR;

namespace AccountContentService.Application.Features.Payments.Commands.CreatePayment
{
    public class CreatePaymentHandler
        : IRequestHandler<CreatePaymentCommand, string>
    {
        private readonly IPaymentGateway _gateway;
        private readonly ISubscriptionRepository _subscriptionRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly ILiveSessionApiClient _liveSessionApiClient;
        private readonly ISystemSettingReposiotry _systemSettingRepo;

        public CreatePaymentHandler(
            IPaymentGateway gateway,
            IPaymentRepository paymentRepo,
            ISubscriptionRepository subscriptionRepo,
            ILiveSessionApiClient liveSessionApiClient,
            ISystemSettingReposiotry systemSettingRepo)
        {
            _gateway = gateway;
            _paymentRepo = paymentRepo;
            _subscriptionRepo = subscriptionRepo;
            _liveSessionApiClient = liveSessionApiClient;
            _systemSettingRepo = systemSettingRepo;
        }

        public async Task<string> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
        {
            string itemName = string.Empty;

            if (request.TargetType.Equals("podcast", StringComparison.OrdinalIgnoreCase))
            {
                var podcast = await _liveSessionApiClient.GetPodcastAsync(request.TargetId, cancellationToken);
                if (podcast == null || !podcast.IsPaid)
                    throw new Exception($"Podcast (ID: {request.TargetId}) không tồn tại hoặc không phải là bản trả phí.");

                var systemFeeSetting = await _systemSettingRepo.GetByKeyAsync("SystemFee", cancellationToken);
                decimal systemFee = 5000m;
                if (systemFeeSetting != null && decimal.TryParse(systemFeeSetting.Value, out var parsedFee))
                {
                    systemFee = parsedFee;
                }

                request.TotalAmount = podcast.Price + systemFee;
                itemName = podcast.Title;
            }
            else
            {
                var existing = await _subscriptionRepo.GetActiveByUserIdAsync(request.UserId, cancellationToken);
                var subscription = await _subscriptionRepo.GetPlanByIdAsync(request.TargetId, cancellationToken);

                if (existing != null)
                {
                    if (subscription != null && subscription.Price > (existing.Plan?.Price ?? 0))
                    {
                        // Allow upgrade
                    }
                    else
                    {
                        throw new SubscriptionAlreadyExistsException(
                            $"Bạn đã có gói \"{existing.Plan?.PlanName}\" đang hoạt động đến {existing.EndDate:dd/MM/yyyy}. Không thể mua thêm gói thấp hơn hoặc tương đương.");
                    }
                }
                if (subscription == null)
                    throw new SubscriptionPlanNotFoundException(
                        $"Gói đăng ký (ID: {request.TargetId}) không tồn tại hoặc đã bị vô hiệu hóa.");

                request.TotalAmount = subscription.Price;
                itemName = subscription.PlanName;
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                TotalAmount = request.TotalAmount,
                Status = PaymentStatus.Pending.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (request.Method.Equals("payos", StringComparison.OrdinalIgnoreCase))
            {
                request.OrderCode = Math.Abs(BitConverter.ToInt64(payment.Id.ToByteArray(), 0));
                request.Description = $"{itemName.ToLower().Replace(" ", "-")}_{payment.Id:N}";
            }
            else
            {
                request.Description = itemName;
            }

            if (request.OrderCode.HasValue)
            {
                payment.SetOrderCode((int)Math.Abs(request.OrderCode.Value % int.MaxValue));
            }

            await _paymentRepo.AddAsync(payment);

            var paymentUrl = await _gateway.CreatePaymentUrlAsync(payment.Id, request);

            return paymentUrl;
        }
    }
}