using AiService.Application.Abstractions.Messaging;
using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Scripts.Commands.GeneratePodcastScript;

public class GeneratePodcastScriptHandler : ICommandHandler<GeneratePodcastScriptCommand, Script>
{
    private readonly IScriptService _scripts;

    public GeneratePodcastScriptHandler(IScriptService scripts)
    {
        _scripts = scripts;
    }

    public Task<Result<Script>> Handle(GeneratePodcastScriptCommand command, CancellationToken cancellationToken)
        => _scripts.GeneratePodcastAsync(
            new GeneratePodcastScriptRequest(
                UserId: command.UserId,
                Topic: command.Topic,
                Title: command.Title,
                ContextType: command.ContextType,
                ModelName: command.ModelName,
                Temperature: command.Temperature,
                MaxTokens: command.MaxTokens,
                EditorInstruction: command.EditorInstruction,
                UseAutoContext: command.UseAutoContext,
                StrictFactMode: command.StrictFactMode),
            cancellationToken);
}

