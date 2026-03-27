using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Themes.Queries.GetAllThemes
{
    public record GetAllThemesQuery() : IRequest<PaginationResult<ThemeDto>>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public record GetAllActiveThemesQuery() : IRequest<PaginationResult<ThemeDto>>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public record GetAllThemesByNameQuery() : IRequest<PaginationResult<ThemeDto>>
    {
        public string Name { get; set; } = string.Empty;
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
