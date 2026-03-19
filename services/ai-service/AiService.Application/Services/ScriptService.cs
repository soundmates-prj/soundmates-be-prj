using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Entities;
using AiService.Domain.Enums;
using AiService.Domain.Interfaces;

namespace AiService.Application.Services;

public class ScriptService : IScriptService
{
    private readonly IAiPromptRepository _prompts;
    private readonly IScriptRepository _scripts;
    private readonly IUnitOfWork _uow;
    private readonly ILlmClient _llm;
    private readonly IUsageService _usage;

    public ScriptService(
        IAiPromptRepository prompts,
        IScriptRepository scripts,
        IUnitOfWork uow,
        ILlmClient llm,
        IUsageService usage)
    {
        _prompts = prompts;
        _scripts = scripts;
        _uow = uow;
        _llm = llm;
        _usage = usage;
    }

    public async Task<Result<Script>> GeneratePodcastAsync(GeneratePodcastScriptRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
            return Result<Script>.Failure("topic is required");
        if (string.IsNullOrWhiteSpace(request.ContextType))
            return Result<Script>.Failure("context_type is required");

        var inputText = request.Topic.Trim();

        var prompt = new AiPrompt
        {
            PromptId = Guid.NewGuid(),
            UserId = request.UserId,
            ContextType = request.ContextType.Trim(),
            InputText = inputText,
            ModelName = request.ModelName,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            CreatedAt = DateTime.UtcNow
        };

        await _prompts.AddAsync(prompt, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var llmResp = await _llm.GenerateAsync(
            new LlmGenerateRequest(
                InputText: inputText,
                ModelName: request.ModelName,
                Temperature: request.Temperature,
                MaxTokens: request.MaxTokens,
                ContextType: request.ContextType.Trim()),
            cancellationToken);

        var script = new Script
        {
            ScriptId = Guid.NewGuid(),
            AuthorId = request.UserId,
            ScriptSource = ScriptSource.Ai.ToString().ToLowerInvariant(),
            ContextType = request.ContextType.Trim(),
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            ContentText = llmResp.ContentText,
            Status = ScriptStatus.Generated.ToString().ToLowerInvariant(),
            PromptId = prompt.PromptId,
            CreatedAt = DateTime.UtcNow
        };

        await _scripts.AddAsync(script, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        if (llmResp.TokensUsed is not null || llmResp.Cost is not null)
        {
            await _usage.LogAsync(new AiUsage
            {
                UsageId = Guid.NewGuid(),
                UserId = request.UserId,
                Provider = "llm",
                TokensUsed = llmResp.TokensUsed,
                Cost = llmResp.Cost,
                ScriptId = script.ScriptId,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        return Result<Script>.Success(script);
    }

    public async Task<Result<IReadOnlyList<Script>>> SplitToAudioPartsAsync(SplitScriptPartsRequest request, CancellationToken cancellationToken)
    {
        if (request.MaxCharsPerPart <= 0)
            return Result<IReadOnlyList<Script>>.Failure("maxCharsPerPart must be > 0");

        var parent = await _scripts.GetByIdAsync(request.ScriptId, cancellationToken);
        if (parent is null)
            return Result<IReadOnlyList<Script>>.Failure("script not found");
        if (parent.AuthorId != request.UserId)
            throw new UnauthorizedAccessException();

        var text = parent.ContentText?.Trim() ?? "";
        if (text.Length == 0)
            return Result<IReadOnlyList<Script>>.Failure("script content is empty");

        var parts = SplitByMaxChars(text, request.MaxCharsPerPart);
        var created = new List<Script>(parts.Count);

        foreach (var part in parts)
        {
            var audioScript = new Script
            {
                ScriptId = Guid.NewGuid(),
                AuthorId = request.UserId,
                ScriptSource = parent.ScriptSource,
                ContextType = "audio",
                Title = parent.Title,
                ContentText = part,
                Status = ScriptStatus.Generated.ToString().ToLowerInvariant(),
                PromptId = parent.PromptId,
                ParentScriptId = parent.ScriptId,
                CreatedAt = DateTime.UtcNow
            };

            await _scripts.AddAsync(audioScript, cancellationToken);
            created.Add(audioScript);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result<IReadOnlyList<Script>>.Success(created);
    }

    public async Task<Result<Script>> GetByIdAsync(Guid scriptId, CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByIdAsync(scriptId, cancellationToken);
        return script is null
            ? Result<Script>.Failure("script not found")
            : Result<Script>.Success(script);
    }

    public async Task<Result<IReadOnlyList<Script>>> GetMyScriptsAsync(Guid userId, string? contextType, string? status, CancellationToken cancellationToken)
    {
        var scripts = await _scripts.GetByAuthorAsync(userId, contextType, status, cancellationToken);
        return Result<IReadOnlyList<Script>>.Success(scripts);
    }

    private static List<string> SplitByMaxChars(string text, int maxChars)
    {
        var parts = new List<string>();
        var remaining = text;

        while (remaining.Length > maxChars)
        {
            var cut = remaining.LastIndexOf('\n', maxChars);
            if (cut < maxChars / 2)
                cut = remaining.LastIndexOf(' ', maxChars);
            if (cut < maxChars / 2)
                cut = maxChars;

            var chunk = remaining[..cut].Trim();
            if (chunk.Length > 0)
                parts.Add(chunk);
            remaining = remaining[cut..].Trim();
        }

        if (remaining.Length > 0)
            parts.Add(remaining);

        return parts;
    }
}

