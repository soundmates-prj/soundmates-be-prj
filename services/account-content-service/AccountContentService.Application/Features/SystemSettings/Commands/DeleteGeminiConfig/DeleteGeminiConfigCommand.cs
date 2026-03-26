using MediatR;

namespace AccountContentService.Application.Features.SystemSettings.Commands.DeleteGeminiConfig;

public sealed record DeleteGeminiConfigCommand : IRequest<bool>;
