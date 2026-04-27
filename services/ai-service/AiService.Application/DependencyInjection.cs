using AiService.Application.Abstractions.Messaging.Dispatcher;
using AiService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AiService.Application.Features.Audios.Commands.GenerateAudio;
using AiService.Application.Features.Audios.Queries.GetAudioById;
using AiService.Application.Features.Scripts.Commands.CreateManualScript;
using AiService.Application.Features.Scripts.Commands.GeneratePodcastScript;
using AiService.Application.Features.Scripts.Commands.SplitScript;
using AiService.Application.Features.Scripts.Commands.DeleteScript;
using AiService.Application.Features.Scripts.Commands.UpdateScript;

using AiService.Application.Features.Scripts.Queries.GetMyScripts;
using AiService.Application.Features.Scripts.Queries.GetScriptById;
using AiService.Application.Features.Voices.Commands.CreateVoice;
using AiService.Application.Features.Voices.Queries.GetActiveVoices;
using AiService.Application.Interfaces;
using AiService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AiService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAiApplication(this IServiceCollection services)
    {
        // Dispatchers
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // Existing app services (handlers can reuse them)
        services.AddScoped<IPromptService, PromptService>();
        services.AddScoped<IScriptService, ScriptService>();
        services.AddScoped<IVoiceService, VoiceService>();
        services.AddScoped<IAudioService, AudioService>();
        services.AddScoped<IUsageService, UsageService>();
        services.AddScoped<IGeminiService, GeminiService>();
        services.AddScoped<IPodcastGenerationService, PodcastGenerationService>();

        // Handlers
        services.AddScoped<Abstractions.Messaging.ICommandHandler<CreateManualScriptCommand, Domain.Entities.Script>, CreateManualScriptHandler>();
        services.AddScoped<Abstractions.Messaging.ICommandHandler<GeneratePodcastScriptCommand, Domain.Entities.Script>, GeneratePodcastScriptHandler>();
        services.AddScoped<Abstractions.Messaging.ICommandHandler<SplitScriptPartsCommand, IReadOnlyList<Domain.Entities.Script>>, SplitScriptPartsHandler>();
        services.AddScoped<Abstractions.Messaging.ICommandHandler<DeleteScriptCommand, bool>, DeleteScriptHandler>();
        services.AddScoped<Abstractions.Messaging.ICommandHandler<UpdateScriptCommand, bool>, UpdateScriptHandler>();
        services.AddScoped<Abstractions.Messaging.ICommandHandler<GenerateAudioFromScriptCommand, Domain.Entities.ScriptAudio>, GenerateAudioFromScriptHandler>();
        services.AddScoped<Abstractions.Messaging.ICommandHandler<CreateVoiceCommand, Domain.Entities.TtsVoice>, CreateVoiceHandler>();

        services.AddScoped<Abstractions.Messaging.IQueryHandler<GetScriptByIdQuery, Domain.Entities.Script>, GetScriptByIdHandler>();
        services.AddScoped<Abstractions.Messaging.IQueryHandler<GetMyScriptsQuery, IReadOnlyList<Domain.Entities.Script>>, GetMyScriptsHandler>();
        services.AddScoped<Abstractions.Messaging.IQueryHandler<GetActiveVoicesQuery, IReadOnlyList<Domain.Entities.TtsVoice>>, GetActiveVoicesHandler>();
        services.AddScoped<Abstractions.Messaging.IQueryHandler<GetAudioByIdQuery, Domain.Entities.ScriptAudio>, GetAudioByIdHandler>();

        return services;
    }
}

