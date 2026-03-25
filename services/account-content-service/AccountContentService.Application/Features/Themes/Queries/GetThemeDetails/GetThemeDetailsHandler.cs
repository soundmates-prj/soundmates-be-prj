using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Themes.Queries.GetUserTheme
{
    public class GetThemeDetailsHandler 
        : IRequestHandler<GetThemeDetailsQuery, ThemeDto>
    {
        private readonly IThemeRepository _themeRepository;
        private readonly IMapper _mapper;

        public GetThemeDetailsHandler(
            IThemeRepository themeRepository,
            IMapper mapper)
        {
            _themeRepository = themeRepository;
            _mapper = mapper;
        }

        public async Task<ThemeDto> Handle(GetThemeDetailsQuery request, CancellationToken cancellationToken)
        {
            var theme = await _themeRepository.GetByIdAsync(request.ThemeId, cancellationToken);

            if (theme == null)
                throw new NotFoundException("No theme found");

            return _mapper.Map<ThemeDto>(theme);
        }
    }
}
