using AccountContentService.Domain.Entities;
using MediatR;
using System.Collections.Generic;

namespace AccountContentService.Application.Features.Payments.Queries.GetAllPendingPayouts;

public sealed record GetAllPendingPayoutsQuery() : IRequest<List<PendingPayoutDto>>;

public class PendingPayoutDto
{
    public System.Guid Id { get; set; }
    public System.Guid PaymentId { get; set; }
    public System.Guid TargetUserId { get; set; }
    public decimal Amount { get; set; }
    public string? BankId { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public System.DateTime ScheduledAt { get; set; }
    public System.DateTime CreatedAt { get; set; }
}
