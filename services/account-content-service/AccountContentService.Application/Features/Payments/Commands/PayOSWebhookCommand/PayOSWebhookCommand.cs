using MediatR;

namespace AccountContentService.Application.Features.Payments.Commands.PayOSWebhookCommand;

public sealed record PayOSWebhookCommand : IRequest<bool>
{
    public string OrderId { get; init; } = string.Empty;
    public string PaymentLinkId { get; init; } = string.Empty;
    public int Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public long? TransactionDateTime { get; init; }
    public string Signature { get; init; } = string.Empty;
}
