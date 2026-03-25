using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AccountContentService.Application.Features.Themes.Commands.UpdateTheme
{
    public class UpdateThemeHandler : IRequestHandler<UpdateThemeCommand, ThemeDto>
    {
        private readonly IThemeRepository _themeRepository;
        private readonly IMapper _mapper;

        public UpdateThemeHandler(IThemeRepository themeRepository, IMapper mapper)
        {
            _themeRepository = themeRepository;
            _mapper = mapper;
        }

        public async Task<ThemeDto> Handle(UpdateThemeCommand request, CancellationToken cancellationToken)
        {
            var theme = await _themeRepository.GetByIdAsync(request.Id, cancellationToken);

            if (theme == null)
                throw new Exception("Theme not found");

            _mapper.Map(request, theme);
            theme.UpdatedAt = DateTime.UtcNow;

            await _themeRepository.UpdateAsync(theme, cancellationToken);

            return _mapper.Map<ThemeDto>(theme);
        }
    }
}
