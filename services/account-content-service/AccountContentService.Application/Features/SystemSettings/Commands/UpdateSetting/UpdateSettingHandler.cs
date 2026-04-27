using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.SystemSettings.Commands.UpdateSetting;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SystemSettings.Commands.UpdateSetting
{
    public class UpdateSettingHandler : IRequestHandler<UpdateSettingCommand, SystemSettingDto>
    {
        private readonly ISystemSettingReposiotry _reposiotry;
        private readonly IMapper _mapper;
        private readonly IEncryptionService _encryption;

        public UpdateSettingHandler(ISystemSettingReposiotry reposiotry, IMapper mapper, IEncryptionService encryption)
        {
            _reposiotry = reposiotry;
            _mapper = mapper;
            _encryption = encryption;
        }

        public async Task<SystemSettingDto> Handle(UpdateSettingCommand request, CancellationToken cancellationToken)
        {
            var existingSetting = await _reposiotry.GetByIdAsync(request.Id, cancellationToken);

            if (existingSetting == null)
                throw new Exception("No setting found");

            // giữ lại value cũ để so sánh
            var oldValue = existingSetting.Value;

            _mapper.Map(request, existingSetting);

            // chỉ xử lý khi value thay đổi
            if (request.Value != oldValue)
            {
                existingSetting.Value = _encryption.Encrypt(existingSetting.Value);
            }
            existingSetting.UpdateAt = DateTime.UtcNow;

            await _reposiotry.UpdateAsync(existingSetting);

            var result = _mapper.Map<SystemSettingDto>(existingSetting);

            result.Value = _encryption.Decrypt(existingSetting.Value);

            return result;
        }
    }
}
