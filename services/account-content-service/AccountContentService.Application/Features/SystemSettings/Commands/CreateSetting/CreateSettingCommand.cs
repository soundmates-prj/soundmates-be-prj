using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SystemSettings.Commands.CreateSetting
{
    public class CreateSettingCommand : IRequest<SystemSettingDto>
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public string SettingType { get; set; } = string.Empty;

        public string Descreption { get; set; } = string.Empty;
    }
}
