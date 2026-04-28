using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AccountContentService.Application.Features.Payments.Queries.GetMyRevenues;

public sealed class GetMyRevenuesHandler : IRequestHandler<GetMyRevenuesQuery, List<RevenueDto>>
{
    private readonly IPendingPayoutRepository _repository;

    public GetMyRevenuesHandler(IPendingPayoutRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<RevenueDto>> Handle(GetMyRevenuesQuery request, CancellationToken cancellationToken)
    {
        var payouts = await _repository.GetByTargetUserIdAsync(request.TargetUserId, cancellationToken);

        return payouts
            .Select(p => new RevenueDto
            {
                Id = p.Id,
                PaymentId = p.PaymentId,
                Amount = p.Amount,
                Status = p.Status,
                CreatedAt = p.CreatedAt
            })
            .ToList();
    }
}
