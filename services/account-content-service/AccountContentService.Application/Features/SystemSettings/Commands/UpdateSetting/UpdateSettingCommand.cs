using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SystemSettings.Commands.UpdateSetting
{
    public class UpdateSettingCommand : IRequest<SystemSettingDto>
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public string SettingType { get; set; } = string.Empty;

        public string Descreption { get; set; } = string.Empty;
    }
}
