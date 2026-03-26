using AccountContentService.Application.Interfaces.Repositories;
using MediatR;

namespace AccountContentService.Application.Features.SystemSettings.Queries.GetGeminiConfig;

public sealed class GetGeminiConfigHandler
    : IRequestHandler<GetGeminiConfigQuery, GetGeminiConfigResult?>
{
    private readonly ISystemSettingReposiotry _settingRepository;

    public GetGeminiConfigHandler(ISystemSettingReposiotry settingRepository)
    {
        _settingRepository = settingRepository;
    }

    public async Task<GetGeminiConfigResult?> Handle(
        GetGeminiConfigQuery request,
        CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetByKeyAsync(
            "gemini:apikey",
            cancellationToken);

        if (setting == null)
        {
            return null;
        }

        // Mask API key — show only last 4 chars
        var value = setting.Value ?? string.Empty;
        var masked = value.Length > 4
            ? new string('*', value.Length - 4) + value[^4..]
            : new string('*', value.Length);

        // isActive is encoded in SettingType: "encrypted:true" or "encrypted:false"
        var isActive = setting.SettingType?.EndsWith("true") == true;

        return new GetGeminiConfigResult(
            Provider: "Gemini",
            MaskedApiKey: masked,
            IsConfigured: true,
            IsActive: isActive,
            UpdatedAt: setting.UpdateAt
        );
    }
}
