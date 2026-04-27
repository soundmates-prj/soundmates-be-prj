using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.DTOs;
using AutoMapper;

namespace AccountContentService.Api.Mappings
{
    public class NotificationMappingProfile : Profile
    {
        public NotificationMappingProfile()
        {
            CreateMap<NotificationDto, NotificationResponse>();
        }
    }
}
