using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Domain.Entities;

namespace AccountContentService.Api.Mappings;

public static class SystemConfigMappings
{
    public static SystemConfigResponse ToResponse(this SystemConfig config)
    {
        return new SystemConfigResponse
        {
            Id = config.Id,
            ConfigKey = config.ConfigKey,
            ConfigValue = config.ConfigValue,
            Category = config.Category,
            Description = config.Description,
            IsEncrypted = config.IsEncrypted,
            IsSensitive = config.IsSensitive,
            IsActive = config.IsActive,
            UpdatedByUserId = config.UpdatedByUserId,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }

    public static List<SystemConfigResponse> ToResponseList(this IEnumerable<SystemConfig> configs)
    {
        return configs.Select(c => c.ToResponse()).ToList();
    }
}
