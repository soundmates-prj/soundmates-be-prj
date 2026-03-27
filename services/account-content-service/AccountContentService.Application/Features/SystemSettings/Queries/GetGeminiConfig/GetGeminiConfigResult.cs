namespace AccountContentService.Application.Features.SystemSettings.Queries.GetGeminiConfig;

public sealed record GetGeminiConfigResult(
    string Provider,
    string MaskedApiKey,
    bool IsConfigured,
    bool IsActive,
    DateTime? UpdatedAt
);
