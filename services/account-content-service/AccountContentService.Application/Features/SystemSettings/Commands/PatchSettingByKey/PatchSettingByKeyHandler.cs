using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using MediatR;

namespace AccountContentService.Application.Features.SystemSettings.Commands.PatchSettingByKey
{
    public class PatchSettingByKeyHandler : IRequestHandler<PatchSettingByKeyCommand, SystemSetting>
    {
        private readonly ISystemSettingReposiotry _repository;

        public PatchSettingByKeyHandler(ISystemSettingReposiotry repository)
        {
            _repository = repository;
        }

        public async Task<SystemSetting> Handle(PatchSettingByKeyCommand request, CancellationToken cancellationToken)
        {
            var setting = await _repository.GetByKeyAsync(request.Key, cancellationToken);
            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    Key = request.Key,
                    Value = request.Value,
                    SettingType = "String",
                    Description = $"Auto generated setting for {request.Key}",
                    CreatedAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                };
                await _repository.AddAsync(setting);
            }
            else
            {
                setting.Value = request.Value;
                setting.UpdateAt = DateTime.UtcNow;
                await _repository.UpdateAsync(setting);
            }

            return setting;
        }
    }
}
