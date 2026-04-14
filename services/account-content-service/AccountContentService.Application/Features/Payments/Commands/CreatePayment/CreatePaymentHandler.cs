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

        public CreatePaymentHandler(
            IPaymentGateway gateway,
            IPaymentRepository paymentRepo,
            ISubscriptionRepository subscriptionRepo)
        {
            _gateway = gateway;
            _paymentRepo = paymentRepo;
            _subscriptionRepo = subscriptionRepo;
        }

        public async Task<string> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
        {
            
            var existing = await _subscriptionRepo.GetActiveByUserIdAsync(request.UserId, cancellationToken);
            var subscription = await _subscriptionRepo.GetPlanByIdAsync(request.TargetId, cancellationToken);

            if (existing != null)
                throw new SubscriptionAlreadyExistsException(
                    $"Bạn đã có gói \"{existing.Plan?.PlanName}\" đang hoạt động đến {existing.EndDate:dd/MM/yyyy}. Không thể mua thêm gói mới.");
            if (subscription == null)
                throw new SubscriptionPlanNotFoundException(
                    $"Gói đăng ký (ID: {request.TargetId}) không tồn tại hoặc đã bị vô hiệu hóa.");

            request.TotalAmount = subscription.Price;

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
                // Use payment.Id as the PayOS orderCode — it's a GUID which is unique and
                // allows us to recover the Payment record directly from the webhook
                // without needing to parse descriptions or rely on matching by timestamp.
                request.OrderCode = Math.Abs(BitConverter.ToInt64(payment.Id.ToByteArray(), 0));

                // Description format: "plantype_paymentguid" — used for reference/debugging.
                request.Description = $"{subscription.PlanName.ToLower().Replace(" ", "-")}_{payment.Id:N}";
            }
            else
            {
                request.Description = subscription.PlanName;
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