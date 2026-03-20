using AiService.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace AiService.Infrastructure.Clients;

public class LlmClientStub : ILlmClient
{
    private readonly LlmOptions _options;

    public LlmClientStub(IOptions<LlmOptions> options)
    {
        _options = options.Value;
    }

    public Task<LlmGenerateResponse> GenerateAsync(LlmGenerateRequest request, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        // Stub behavior: if no API key configured, return a deterministic script template.
        // This keeps the service runnable while you wire up a real provider client.
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || _options.ApiKey.StartsWith("${") || _options.ApiKey.Contains("<"))
        {
            var content =
                $"[Podcast Script - {request.ContextType}]\n\n" +
                $"Chủ đề: {request.InputText}\n\n" +
                "Mở đầu:\n" +
                "Xin chào mọi người, chào mừng bạn đến với SoundMates.\n\n" +
                "Nội dung chính:\n" +
                "- Điểm 1: Giới thiệu khái niệm liên quan đến chủ đề.\n" +
                "- Điểm 2: Ví dụ thực tế và ứng dụng.\n" +
                "- Điểm 3: Lời khuyên và lưu ý.\n\n" +
                "Kết:\n" +
                "Cảm ơn bạn đã lắng nghe. Hẹn gặp lại!\n";

            return Task.FromResult(new LlmGenerateResponse(ContentText: content));
        }

        throw new NotImplementedException("LLM provider client is not implemented yet. Configure a real LLM client implementation.");
    }
}

