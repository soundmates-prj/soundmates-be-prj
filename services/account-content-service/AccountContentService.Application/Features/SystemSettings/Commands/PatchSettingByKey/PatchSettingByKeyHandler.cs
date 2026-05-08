using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using MediatR;
using AutoMapper;
using AccountContentService.Application.DTOs;

namespace AccountContentService.Application.Features.SystemSettings.Commands.PatchSettingByKey
{
    public class PatchSettingByKeyHandler : IRequestHandler<PatchSettingByKeyCommand, SystemSettingDto>
    {
        private readonly ISystemSettingReposiotry _repository;
        private readonly IMapper _mapper;

        public PatchSettingByKeyHandler(ISystemSettingReposiotry repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<SystemSettingDto> Handle(PatchSettingByKeyCommand request, CancellationToken cancellationToken)
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

            return _mapper.Map<SystemSettingDto>(setting);
        }
    }
}
