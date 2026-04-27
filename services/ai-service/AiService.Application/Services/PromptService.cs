using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Entities;
using AiService.Domain.Interfaces;

namespace AiService.Application.Services;

public class PromptService : IPromptService
{
    private readonly IAiPromptRepository _prompts;
    private readonly IUnitOfWork _uow;

    public PromptService(IAiPromptRepository prompts, IUnitOfWork uow)
    {
        _prompts = prompts;
        _uow = uow;
    }

    public async Task<Result<AiPrompt>> CreateAsync(CreatePromptRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ContextType))
            return Result<AiPrompt>.Failure("context_type is required");
        if (string.IsNullOrWhiteSpace(request.InputText))
            return Result<AiPrompt>.Failure("input_text is required");

        var prompt = new AiPrompt
        {
            PromptId = Guid.NewGuid(),
            UserId = request.UserId,
            ContextType = request.ContextType.Trim(),
            InputText = request.InputText,
            ModelName = request.ModelName,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            CreatedAt = DateTime.UtcNow
        };

        await _prompts.AddAsync(prompt, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<AiPrompt>.Success(prompt);
    }
}

