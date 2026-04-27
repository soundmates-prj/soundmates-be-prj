using MediatR;

namespace AccountContentService.Application.Features.ServiceConfigs.Commands.UpsertAzuraCastConfig;

public sealed record UpsertAzuraCastConfigCommand(
    string BaseUrl,
    string ApiKey,
    bool IsActive) : IRequest<UpsertAzuraCastConfigResult>;

public sealed record UpsertAzuraCastConfigResult(
    string BaseUrl,
    bool IsConfigured,
    bool IsActive,
    DateTime UpdatedAt);
