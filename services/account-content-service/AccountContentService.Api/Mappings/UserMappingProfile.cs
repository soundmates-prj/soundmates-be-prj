using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Mappings
{
    
        public class UserMappingProfile : Profile
        {
            public UserMappingProfile()
            {
                CreateMap<DTOs.UserProfileDto, DTOs.UserProfileDto>();
            }
        }
    
}
