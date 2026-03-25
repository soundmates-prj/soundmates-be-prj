using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.BlogComments.Commands.ReplyComment;
using AccountContentService.Application.Features.BlogComments.Commands.UpdateComment;
using AccountContentService.Application.Features.Themes.Commands.CreateTheme;
using AccountContentService.Application.Features.Themes.Commands.UpdateTheme;
using AccountContentService.Domain.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Mappings
{
    public class ThemeProfile : Profile
    {
        public ThemeProfile()
        {
            CreateMap<Theme, ThemeDto>();
            CreateMap<CreateThemeCommand, Theme>();
            CreateMap<UpdateThemeCommand, Theme>()
                .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
