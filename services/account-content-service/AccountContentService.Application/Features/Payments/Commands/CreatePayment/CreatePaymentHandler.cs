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
            // 🔥 1. Check user đã có subscription active chưa
            var existing = await _subscriptionRepo.GetActiveByUserIdAsync(request.UserId, cancellationToken);

            if (existing != null)
                throw new Exception("User already has active subscription");

            // 🔥 2. Tạo Payment (pending)
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

            await _paymentRepo.AddAsync(payment);

            var paymentUrl = await _gateway.CreatePaymentUrlAsync(payment.Id, request);

            return paymentUrl;
        }
    }
}