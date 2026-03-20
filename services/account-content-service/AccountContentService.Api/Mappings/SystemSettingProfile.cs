using AccountContentService.Api.Common;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.SystemSettings.Commands.CreateSetting;
using AccountContentService.Application.Features.SystemSettings.Commands.UpdateSetting;
using AccountContentService.Application.Features.SystemSettings.Queries.GetSettings;
using AutoMapper;

namespace AccountContentService.Api.Mappings
{
    public class SystemSettingProfile : Profile
    {
        public SystemSettingProfile()
        {
            CreateMap<SystemSettingDto, SystemSettingResponse>();
            CreateMap<SystemSettingRequest, CreateSettingCommand>();
            CreateMap<SystemSettingRequest, UpdateSettingCommand>();

            CreateMap<PaginationNoFilterRequest, GetSettingsQuery>();
        }
    }
}
