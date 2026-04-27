using AiService.Application.Enums;
using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Entities;
using AiService.Domain.Enums;
using AiService.Domain.Interfaces;

namespace AiService.Application.Services;

public class ScriptService : IScriptService
{
    private const int MinPodcastChars = 2500;
    private const int MaxContinuationAttempts = 5;

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

        var topic = request.Topic.Trim();
        var editorInstruction = NormalizeEditorInstruction(request.EditorInstruction);
        var inputText = BuildPodcastInputPrompt(topic, request.Title, request.MaxTokens, request.UseAutoContext, editorInstruction);
        var systemPrompt = BuildPodcastSystemPrompt(request.StrictFactMode, request.UseAutoContext, editorInstruction);

        var prompt = new AiPrompt
        {
            PromptId = Guid.NewGuid(),
            UserId = request.UserId,
            ContextType = request.ContextType.Trim(),
            InputText = topic,
            ModelName = request.ModelName,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            CreatedAt = DateTime.UtcNow
        };

        await _prompts.AddAsync(prompt, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var generation = await GeneratePodcastContentAsync(
            topic,
            request.Title,
            request.ModelName,
            request.Temperature,
            request.MaxTokens,
            request.ContextType.Trim(),
            inputText,
            systemPrompt,
            editorInstruction,
            request.StrictFactMode,
            cancellationToken);

        var finalContent = generation.Content;

        var script = new Script
        {
            ScriptId = Guid.NewGuid(),
            AuthorId = request.UserId,
            ScriptSource = ScriptSource.Ai.ToString().ToLowerInvariant(),
            ContextType = request.ContextType.Trim(),
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            ContentText = finalContent,
            Status = ScriptStatus.Generated.ToString().ToLowerInvariant(),
            PromptId = prompt.PromptId,
            CreatedAt = DateTime.UtcNow
        };

        await _scripts.AddAsync(script, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        if (generation.TokensUsed is not null || generation.Cost is not null)
        {
            await _usage.LogAsync(new AiUsage
            {
                UsageId = Guid.NewGuid(),
                UserId = request.UserId,
                Provider = "llm",
                TokensUsed = generation.TokensUsed,
                Cost = generation.Cost,
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

    public async Task<Result<bool>> DeleteAsync(Guid userId, Guid scriptId, CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByIdAsync(scriptId, cancellationToken);
        if (script is null)
            return Result<bool>.Failure("script not found", (int)ApiStatusCode.HB40401);

        if (script.AuthorId != userId)
            return Result<bool>.Failure("forbidden", (int)ApiStatusCode.HB40301);

        await _scripts.DeleteAsync(scriptId, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> UpdateAsync(Guid userId, Guid scriptId, string? title, string? contentText, CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByIdAsync(scriptId, cancellationToken);
        if (script is null)
            return Result<bool>.Failure("script not found", (int)ApiStatusCode.HB40401);

        if (script.AuthorId != userId)
            return Result<bool>.Failure("forbidden", (int)ApiStatusCode.HB40301);

        if (!string.IsNullOrWhiteSpace(title))
            script.Title = title.Trim();
        if (!string.IsNullOrWhiteSpace(contentText))
            script.ContentText = contentText.Trim();

        await _scripts.UpdateAsync(script, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Creates a script directly from user-provided content, bypassing AI generation entirely.
    /// </summary>
    public async Task<Result<Script>> CreateManualAsync(Guid userId, string contentText, string? title, string? topic, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contentText))
            return Result<Script>.Failure("contentText is required");

        var script = new Script
        {
            ScriptId = Guid.NewGuid(),
            AuthorId = userId,
            ScriptSource = "manual",
            ContextType = "podcast",
            Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            ContentText = contentText.Trim(),
            Status = Domain.Enums.ScriptStatus.Generated.ToString().ToLowerInvariant(),
            PromptId = null,
            CreatedAt = DateTime.UtcNow
        };

        await _scripts.AddAsync(script, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Script>.Success(script);
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

    private static string BuildPodcastSystemPrompt(bool strictFactMode, bool useAutoContext, string? editorInstruction)
    {
        var strictFactInstruction = strictFactMode
            ? "Chế độ strict fact đang bật: chỉ nêu thông tin có cơ sở. Nếu dữ kiện chưa chắc chắn, dùng cách diễn đạt thận trọng như 'theo nhiều tài liệu' và không bịa số liệu cụ thể."
            : "";

        var contextInstruction = useAutoContext
            ? "Sử dụng context hệ thống như một khung tham chiếu, nhưng vẫn ưu tiên yêu cầu biên tập của người dùng."
            : "Bỏ qua mọi khuôn mẫu context mặc định, ưu tiên hoàn toàn định hướng biên tập do người dùng/staff cung cấp."
            ;

        var editorInstructionText = string.IsNullOrWhiteSpace(editorInstruction)
            ? ""
            : $"Yêu cầu biên tập từ staff: {editorInstruction}";

        return $"Bạn là biên tập viên podcast tiếng Việt. Viết nội dung mạch lạc, dễ nghe khi đọc thành audio. Không trả lời quá ngắn hoặc bỏ dở câu. {contextInstruction} {strictFactInstruction} {editorInstructionText}".Trim();
    }

    private static string BuildPodcastInputPrompt(string topic, string? title, int? maxTokens, bool useAutoContext, string? editorInstruction)
    {
        var effectiveTokens = maxTokens.GetValueOrDefault(1200);
        var targetWords = EstimateTargetWordsFromTokens(effectiveTokens);
        var safeTitle = string.IsNullOrWhiteSpace(title) ? topic : title.Trim();

        if (!useAutoContext)
        {
            if (!string.IsNullOrWhiteSpace(editorInstruction))
            {
                return $"""
                    Viết kịch bản podcast tiếng Việt theo đúng yêu cầu biên tập sau.

                    Tiêu đề: {safeTitle}
                    Chủ đề: {topic}
                    Yêu cầu biên tập:
                    {editorInstruction}

                    Ràng buộc:
                    - Độ dài mục tiêu khoảng {targetWords} từ.
                    - Trả về duy nhất nội dung script hoàn chỉnh.
                    """;
            }

            return $"""
                Viết một kịch bản podcast tiếng Việt hoàn chỉnh.
                Tiêu đề: {safeTitle}
                Chủ đề: {topic}
                Độ dài mục tiêu khoảng {targetWords} từ.
                Chỉ trả về nội dung script.
                """;
        }

        return $"""
            Hãy viết một kịch bản podcast tiếng Việt hoàn chỉnh.

            Tiêu đề: {safeTitle}
            Chủ đề: {topic}

            Yêu cầu:
            - Độ dài mục tiêu khoảng {targetWords} từ.
            - Có cấu trúc rõ: Mở đầu, 3-4 ý chính, Kết luận.
            - Mỗi ý chính có giải thích ngắn và ví dụ thực tế tại Việt Nam.
            - Văn phong tự nhiên, phù hợp đọc thành audio.
            - Kết thúc bằng 1-2 câu tổng kết trọn vẹn.
                {(string.IsNullOrWhiteSpace(editorInstruction) ? "" : $"- Yêu cầu biên tập thêm từ staff: {editorInstruction}")}
            - Chỉ trả về nội dung script, không thêm ghi chú hệ thống.
            """;
    }

    private static int EstimateTargetWordsFromTokens(int maxTokens)
    {
        // Keep the prompt target realistic with maxOutputTokens to reduce mid-sentence truncation.
        var estimatedWords = (int)Math.Round(maxTokens * 0.62m, MidpointRounding.AwayFromZero);
        return Math.Clamp(estimatedWords, 220, 1200);
    }

    private static string BuildPodcastContinuationPrompt(string topic, string? title, string currentContent, int minChars, int attempt, string? editorInstruction, bool strictFactMode)
    {
        var safeTitle = string.IsNullOrWhiteSpace(title) ? topic : title.Trim();
        var strictFactInstruction = strictFactMode
            ? "- Nếu có dữ kiện chưa chắc chắn thì diễn đạt thận trọng, không bịa số liệu."
            : "";
        var editorInstructionText = string.IsNullOrWhiteSpace(editorInstruction)
            ? ""
            : $"- Tiếp tục tuân thủ yêu cầu biên tập: {editorInstruction}";

        return $"""
            Bạn đang viết dở kịch bản podcast và nội dung hiện tại còn quá ngắn.

            Tiêu đề: {safeTitle}
            Chủ đề: {topic}
            Nội dung hiện có:
            {currentContent}

            Lần viết tiếp: {attempt}/{MaxContinuationAttempts}

            Hãy viết tiếp để tổng nội dung dài ít nhất {minChars} ký tự.
            Yêu cầu:
            - Không lặp lại nguyên văn đoạn đã có.
            - Không lặp lại phần mở đầu, chuyển ý ngay từ nội dung đang dở.
            - Viết liền mạch, tự nhiên, kết thúc trọn ý.
            {strictFactInstruction}
            {editorInstructionText}
            - Chỉ trả về phần viết tiếp.
            """;
    }

    private static string? NormalizeEditorInstruction(string? editorInstruction)
    {
        if (string.IsNullOrWhiteSpace(editorInstruction))
        {
            return null;
        }

        var normalized = editorInstruction.Trim();
        return normalized.Length > 2000 ? normalized[..2000] : normalized;
    }

    private static string NormalizeGeneratedText(string? content)
    {
        return (content ?? string.Empty).Trim();
    }

    private async Task<(string Content, int? TokensUsed, decimal? Cost)> GeneratePodcastContentAsync(
        string topic,
        string? title,
        string? modelName,
        decimal? temperature,
        int? maxTokens,
        string contextType,
        string initialPrompt,
        string systemPrompt,
        string? editorInstruction,
        bool strictFactMode,
        CancellationToken cancellationToken)
    {
        var response = await _llm.GenerateAsync(
            new LlmGenerateRequest(
                InputText: initialPrompt,
                ModelName: modelName,
                Temperature: temperature,
                MaxTokens: maxTokens,
                ContextType: contextType,
                SystemPrompt: systemPrompt),
            cancellationToken);

        var content = NormalizeGeneratedText(response.ContentText);
        var tokensUsed = response.TokensUsed;
        var cost = response.Cost;

        for (var attempt = 1; attempt <= MaxContinuationAttempts && content.Length < MinPodcastChars; attempt++)
        {
            var continuePrompt = BuildPodcastContinuationPrompt(topic, title, content, MinPodcastChars, attempt, editorInstruction, strictFactMode);
            var continueResponse = await _llm.GenerateAsync(
                new LlmGenerateRequest(
                    InputText: continuePrompt,
                    ModelName: modelName,
                    Temperature: temperature,
                    MaxTokens: maxTokens,
                    ContextType: contextType,
                    SystemPrompt: systemPrompt),
                cancellationToken);

            var extra = NormalizeGeneratedText(continueResponse.ContentText);
            if (string.IsNullOrWhiteSpace(extra))
            {
                break;
            }

            tokensUsed ??= continueResponse.TokensUsed;
            cost ??= continueResponse.Cost;

            content = MergeWithoutOverlap(content, extra);
        }

        return (content, tokensUsed, cost);
    }

    private static string MergeWithoutOverlap(string existing, string addition)
    {
        if (string.IsNullOrWhiteSpace(existing))
        {
            return addition.Trim();
        }

        if (string.IsNullOrWhiteSpace(addition))
        {
            return existing.Trim();
        }

        var left = existing.Trim();
        var right = addition.Trim();

        // Remove duplicated overlap where the continuation starts with the same suffix from existing text.
        var maxOverlap = Math.Min(left.Length, right.Length);
        var overlap = 0;
        for (var len = maxOverlap; len >= 40; len--)
        {
            if (left.EndsWith(right[..len], StringComparison.OrdinalIgnoreCase))
            {
                overlap = len;
                break;
            }
        }

        if (overlap > 0)
        {
            right = right[overlap..].TrimStart();
        }

        if (string.IsNullOrWhiteSpace(right))
        {
            return left;
        }

        return string.Concat(left, "\n\n", right).Trim();
    }
}

