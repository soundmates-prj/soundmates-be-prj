using MediatR;

namespace AccountContentService.Application.Features.ServiceConfigs.Commands.DeleteAzuraCastConfig;

public sealed record DeleteAzuraCastConfigCommand : IRequest<bool>;
