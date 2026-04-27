using MediatR;

namespace AccountContentService.Application.Features.SystemSettings.Commands.UpsertGeminiKey;

public sealed record UpsertGeminiKeyCommand(
    string Provider,
    string ApiKey,
    bool IsActive) : IRequest<UpsertGeminiKeyResult>;

public sealed record UpsertGeminiKeyResult(
    string Provider,
    bool IsActive,
    DateTime UpdatedAt);
