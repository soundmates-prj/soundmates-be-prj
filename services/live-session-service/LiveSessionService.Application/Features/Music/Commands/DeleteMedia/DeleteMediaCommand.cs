using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.Music.Commands.DeleteMedia;

public sealed record DeleteMediaCommand(Guid MediaFileId) : ICommand;
