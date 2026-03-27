using System.Text.Json;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountContentService.Application.Features.ServiceConfigs.Commands.UpsertAzuraCastConfig;

public sealed class UpsertAzuraCastConfigHandler
    : IRequestHandler<UpsertAzuraCastConfigCommand, UpsertAzuraCastConfigResult>
{
    private const string ConfigKey = "azuracast:apikey";

    private readonly ISystemSettingReposiotry _settingRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IAzuraCastValidator _validator;
    private readonly IAzuraCastConfigEventPublisher _eventPublisher;
    private readonly ILogger<UpsertAzuraCastConfigHandler> _logger;

    public UpsertAzuraCastConfigHandler(
        ISystemSettingReposiotry settingRepository,
        IEncryptionService encryptionService,
        IAzuraCastValidator validator,
        IAzuraCastConfigEventPublisher eventPublisher,
        ILogger<UpsertAzuraCastConfigHandler> logger)
    {
        _settingRepository = settingRepository;
        _encryptionService = encryptionService;
        _validator = validator;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<UpsertAzuraCastConfigResult> Handle(
        UpsertAzuraCastConfigCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate URL format
        if (string.IsNullOrWhiteSpace(request.BaseUrl))
            throw new ArgumentException("AzuraCast base URL is required.");

        if (!Uri.TryCreate(request.BaseUrl.TrimEnd('/'), UriKind.Absolute, out var baseUri))
            throw new ArgumentException("Invalid AzuraCast base URL format. Must be a valid absolute URL.");

        if (string.IsNullOrWhiteSpace(request.ApiKey))
            throw new ArgumentException("AzuraCast API key is required.");

        var apiKey = request.ApiKey.Trim();
        var baseUrl = baseUri.ToString().TrimEnd('/');

        // 2. Test API key against AzuraCast
        await _validator.ValidateAsync(baseUrl, apiKey, cancellationToken);

        // 3. Encrypt and store in SystemSettings
        var encryptedApiKey = _encryptionService.Encrypt(apiKey);
        var now = DateTime.UtcNow;
        var storedPayload = new AzuraCastStoredPayload(baseUrl, encryptedApiKey);
        var storedJson = JsonSerializer.Serialize(storedPayload);
        var settingType = request.IsActive ? "encrypted:true" : "encrypted:false";
        var description = $"AzuraCast API Key - BaseUrl: {baseUrl}";

        await _settingRepository.UpsertAsync(
            ConfigKey,
            storedJson,
            settingType,
            description,
            request.IsActive,
            cancellationToken);

        _logger.LogInformation(
            "AzuraCast config saved (BaseUrl={BaseUrl}, IsActive={IsActive})",
            baseUrl, request.IsActive);

        // 4. Publish event to RabbitMQ
        try
        {
            await _eventPublisher.PublishUpdatedAsync(
                baseUrl, apiKey, request.IsActive, isDeleted: false, now, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish AzuraCast config event. Data was saved to DB.");
        }

        return new UpsertAzuraCastConfigResult(baseUrl, IsConfigured: true, request.IsActive, now);
    }

    private record AzuraCastStoredPayload(string BaseUrl, string EncryptedApiKey);
}
