using System.Text.Json;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountContentService.Application.Features.ServiceConfigs.Queries.GetAzuraCastConfig;

public sealed class GetAzuraCastConfigHandler
    : IRequestHandler<GetAzuraCastConfigQuery, GetAzuraCastConfigResult>
{
    private const string ConfigKey = "azuracast:apikey";

    private readonly ISystemSettingReposiotry _settingRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<GetAzuraCastConfigHandler> _logger;

    public GetAzuraCastConfigHandler(
        ISystemSettingReposiotry settingRepository,
        IEncryptionService encryptionService,
        ILogger<GetAzuraCastConfigHandler> logger)
    {
        _settingRepository = settingRepository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<GetAzuraCastConfigResult> Handle(
        GetAzuraCastConfigQuery request,
        CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetByKeyAsync(ConfigKey, cancellationToken);

        if (setting is null)
        {
            return new GetAzuraCastConfigResult(
                BaseUrl: string.Empty,
                MaskedApiKey: string.Empty,
                IsConfigured: false,
                IsActive: false,
                UpdatedAt: null);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<AzuraCastStoredPayload>(setting.Value);

            if (payload is null)
            {
                _logger.LogWarning("AzuraCast config found but payload is invalid JSON");
                return new GetAzuraCastConfigResult(
                    BaseUrl: string.Empty,
                    MaskedApiKey: string.Empty,
                    IsConfigured: false,
                    IsActive: false,
                    UpdatedAt: setting.UpdateAt);
            }

            var decryptedKey = _encryptionService.Decrypt(payload.EncryptedApiKey);
            var maskedKey = MaskApiKey(decryptedKey);

            return new GetAzuraCastConfigResult(
                BaseUrl: payload.BaseUrl,
                MaskedApiKey: maskedKey,
                IsConfigured: true,
                IsActive: setting.IsActive,
                UpdatedAt: setting.UpdateAt);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize AzuraCast config payload");
            return new GetAzuraCastConfigResult(
                BaseUrl: string.Empty,
                MaskedApiKey: string.Empty,
                IsConfigured: false,
                IsActive: false,
                UpdatedAt: setting.UpdateAt);
        }
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey)) return string.Empty;
        if (apiKey.Length <= 4) return new string('*', apiKey.Length);
        return new string('*', apiKey.Length - 4) + apiKey[^4..];
    }

    private record AzuraCastStoredPayload(string BaseUrl, string EncryptedApiKey);
}
