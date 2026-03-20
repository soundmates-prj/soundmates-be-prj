using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AccountContentService.Application.Features.SystemSettings.Commands.CreateSetting
{
    public class CreateSettingHandler : IRequestHandler<CreateSettingCommand, SystemSettingDto>
    {
        private readonly ISystemSettingReposiotry _reposiotry;
        private readonly IMapper _mapper;
        private readonly IEncryptionService _encryption;

        public CreateSettingHandler(ISystemSettingReposiotry reposiotry, IMapper mapper, IEncryptionService encryption)
        {
            _reposiotry = reposiotry;
            _mapper = mapper;
            _encryption = encryption;
        }

        public async Task<SystemSettingDto> Handle(CreateSettingCommand request, CancellationToken cancellationToken)
        {
            var existingSetting = await _reposiotry.GetByKeyAsync(request.Key, cancellationToken);
            if (existingSetting != null)
            {
                throw new Exception("This setting is already set");
            }
            var systemSetting = _mapper.Map<SystemSetting>(request);

            systemSetting.Value = _encryption.Encrypt(systemSetting.Value);

            systemSetting.CreatedAt = DateTime.UtcNow;

            await _reposiotry.AddAsync(systemSetting);

            var result = _mapper.Map<SystemSettingDto>(systemSetting);

            result.Value = _encryption.Decrypt(systemSetting.Value);

            return result;
        }
    }
}
