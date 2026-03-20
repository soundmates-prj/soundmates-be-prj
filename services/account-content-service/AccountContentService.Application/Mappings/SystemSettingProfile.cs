using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.SystemSettings.Commands.CreateSetting;
using AccountContentService.Application.Features.SystemSettings.Commands.UpdateSetting;
using AccountContentService.Domain.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Mappings
{
    public class SystemSettingProfile : Profile
    {
        public SystemSettingProfile()
        {
            CreateMap<SystemSetting, SystemSettingDto>();
            CreateMap<CreateSettingCommand, SystemSetting>();
            CreateMap<UpdateSettingCommand, SystemSetting>()
                .ForAllMembers(opts =>
                 opts.Condition((src, dest, srcMember) => srcMember != null));

        }
    }
}
