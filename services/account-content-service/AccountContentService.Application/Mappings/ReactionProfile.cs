using AccountContentService.Application.DTOs;
using AccountContentService.Domain.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Mappings
{
    public class ReactionProfile : Profile
    {
        public ReactionProfile()
        {
            CreateMap<PostReaction, ReactionDto>();
        }
    }
}
