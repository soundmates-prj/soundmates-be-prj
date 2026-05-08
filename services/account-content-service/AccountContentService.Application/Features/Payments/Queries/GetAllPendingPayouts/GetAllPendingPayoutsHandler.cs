using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AccountContentService.Application.Features.Payments.Queries.GetAllPendingPayouts;

public sealed class GetAllPendingPayoutsHandler : IRequestHandler<GetAllPendingPayoutsQuery, List<PendingPayoutDto>>
{
    private readonly IPendingPayoutRepository _repository;
    private readonly IUserProfileReadModelRepository _userProfileRepository;
    private readonly IPaymentRepository _paymentRepository;

    public GetAllPendingPayoutsHandler(
        IPendingPayoutRepository repository,
        IUserProfileReadModelRepository userProfileRepository,
        IPaymentRepository paymentRepository)
    {
        _repository = repository;
        _userProfileRepository = userProfileRepository;
        _paymentRepository = paymentRepository;
    }

    public async Task<List<PendingPayoutDto>> Handle(GetAllPendingPayoutsQuery request, CancellationToken cancellationToken)
    {
        var payouts = await _repository.GetAllPendingPayoutsAsync(cancellationToken);

        var targetUserIds = payouts.Select(p => p.TargetUserId).Distinct().ToList();
        var userProfiles = await _userProfileRepository.GetByIdsAsync(targetUserIds, cancellationToken);
        var userProfileDict = userProfiles.ToDictionary(u => u.Id);

        var paymentIds = payouts.Select(p => p.PaymentId).Distinct().ToList();
        var payments = await _paymentRepository.GetPaymentsByIdsAsync(paymentIds, cancellationToken);
        var paymentDict = payments.ToDictionary(p => p.Id);

        return payouts.Select(p => 
        {
            var user = userProfileDict.GetValueOrDefault(p.TargetUserId);
            var payment = paymentDict.GetValueOrDefault(p.PaymentId);

            var systemAmount = payment != null ? Math.Max(0, payment.Amount - p.Amount) : 0;

            return new PendingPayoutDto
            {
                Id = p.Id,
                PaymentId = p.PaymentId,
                TargetUserId = p.TargetUserId,
                Amount = p.Amount,
                BankId = p.BankId,
                AccountNumber = p.AccountNumber,
                AccountName = p.AccountName,
                Status = p.Status,
                ErrorMessage = p.ErrorMessage,
                ScheduledAt = p.ScheduledAt,
                CreatedAt = p.CreatedAt,
                AuthorFullName = user?.FullName,
                AuthorUsername = user?.Email, // Assuming email as username since Username is not in UserProfileReadModel
                AuthorAvatarUrl = user?.AvatarUrl,
                SystemAmount = systemAmount
            };
        }).ToList();
    }
}
