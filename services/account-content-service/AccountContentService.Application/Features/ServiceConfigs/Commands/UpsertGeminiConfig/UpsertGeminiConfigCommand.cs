using MediatR;

namespace AccountContentService.Application.Features.ServiceConfigs.Commands.UpsertGeminiConfig;

public sealed record UpsertGeminiConfigCommand(
    string Provider,
    string ApiKey,
    bool IsActive) : IRequest<UpsertGeminiConfigResult>;

public sealed record UpsertGeminiConfigResult(
    string Provider,
    bool IsActive,
    DateTime UpdatedAt);
