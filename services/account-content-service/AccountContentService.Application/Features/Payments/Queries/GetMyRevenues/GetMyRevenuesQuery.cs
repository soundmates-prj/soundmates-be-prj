using MediatR;
using System.Collections.Generic;

namespace AccountContentService.Application.Features.Payments.Queries.GetMyRevenues;

public sealed record GetMyRevenuesQuery(System.Guid TargetUserId) : IRequest<List<RevenueDto>>;

public class RevenueDto
{
    public System.Guid Id { get; set; }
    public System.Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public System.DateTime CreatedAt { get; set; }
}
