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

    public GetAllPendingPayoutsHandler(IPendingPayoutRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PendingPayoutDto>> Handle(GetAllPendingPayoutsQuery request, CancellationToken cancellationToken)
    {
        var payouts = await _repository.GetAllPendingPayoutsAsync(cancellationToken);

        return payouts.Select(p => new PendingPayoutDto
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
            CreatedAt = p.CreatedAt
        }).ToList();
    }
}
