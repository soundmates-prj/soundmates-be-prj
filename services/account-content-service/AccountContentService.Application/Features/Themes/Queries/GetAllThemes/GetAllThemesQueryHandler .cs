using AccountContentService.Application.Abstractions;
using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Common.Result;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Themes.Queries.GetAllThemes
{
    public class GetAllThemesQueryHandler 
        : IRequestHandler<GetAllThemesQuery, PaginationResult<ThemeDto>>,
          IRequestHandler<GetAllThemesByNameQuery, PaginationResult<ThemeDto>>,
          IRequestHandler<GetAllActiveThemesQuery, PaginationResult<ThemeDto>>
    {
        private readonly IThemeRepository _themeRepository;
        private readonly IMapper _mapper;

        public GetAllThemesQueryHandler(
            IThemeRepository themeRepository,
            IMapper mapper)
        {
            _themeRepository = themeRepository;
            _mapper = mapper;
        }

        public async Task<PaginationResult<ThemeDto>> Handle(GetAllThemesQuery request, CancellationToken cancellationToken)
        {
            var result = await _themeRepository.GetAllAsync(request.Page, request.PageSize, cancellationToken);

            var items = _mapper.Map<IEnumerable<ThemeDto>>(result.Items);

            return new PaginationResult<ThemeDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<PaginationResult<ThemeDto>> Handle(GetAllActiveThemesQuery request, CancellationToken cancellationToken)
        {
            var result = await _themeRepository.GetActiveAsync(request.Page, request.PageSize, cancellationToken);

            var items = _mapper.Map<IEnumerable<ThemeDto>>(result.Items);

            return new PaginationResult<ThemeDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<PaginationResult<ThemeDto>> Handle(GetAllThemesByNameQuery request, CancellationToken cancellationToken)
        {
            var result = await _themeRepository.GetAllByNameAsync(request.Name, request.Page, request.PageSize, cancellationToken);

            var items = _mapper.Map<IEnumerable<ThemeDto>>(result.Items);

            return new PaginationResult<ThemeDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
