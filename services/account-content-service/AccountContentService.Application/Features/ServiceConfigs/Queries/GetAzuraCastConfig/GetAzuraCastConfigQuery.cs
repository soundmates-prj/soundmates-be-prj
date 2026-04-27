using MediatR;

namespace AccountContentService.Application.Features.ServiceConfigs.Queries.GetAzuraCastConfig;

public sealed record GetAzuraCastConfigQuery : IRequest<GetAzuraCastConfigResult>;

public sealed record GetAzuraCastConfigResult(
    string BaseUrl,
    string MaskedApiKey,
    bool IsConfigured,
    bool IsActive,
    DateTime? UpdatedAt);
