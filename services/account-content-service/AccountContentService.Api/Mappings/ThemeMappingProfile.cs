using AccountContentService.Api.Common;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.PaymentTransactions.Queries.GetTransactions;
using AccountContentService.Application.Features.Themes.Commands.CreateTheme;
using AccountContentService.Application.Features.Themes.Commands.UpdateTheme;
using AccountContentService.Application.Features.Themes.Queries.GetAllThemes;
using AutoMapper;

namespace AccountContentService.Api.Mappings
{
    public class ThemeMappingProfile : Profile
    {
        public ThemeMappingProfile()
        {
            CreateMap<ThemeDto, ThemeResponse>();
            CreateMap<ThemeRequest, CreateThemeCommand>();
            CreateMap<ThemeRequest, UpdateThemeCommand>()
                .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<PaginationNoFilterRequest, GetAllThemesByNameQuery>();
            CreateMap<PaginationNoFilterRequest, GetAllActiveThemesQuery>();
            CreateMap<PaginationNoFilterRequest, GetAllThemesQuery>();
        }
    }
}
