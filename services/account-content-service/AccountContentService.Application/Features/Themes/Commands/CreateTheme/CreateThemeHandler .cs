using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Themes.Commands.CreateTheme
{
    public class CreateThemeHandler : IRequestHandler<CreateThemeCommand, ThemeDto>
    {
        private readonly IThemeRepository _themeRepository;
        private readonly IMapper _mapper;

        public CreateThemeHandler(IThemeRepository themeRepository, IMapper mapper)
        {
            _themeRepository = themeRepository;
            _mapper = mapper;
        }

        public async Task<ThemeDto> Handle(CreateThemeCommand request, CancellationToken cancellationToken)
        {
            var exists = await _themeRepository.ExistsByNameAsync(request.Name, cancellationToken);

            if (exists)
                throw new Exception("Theme name already exists");

            var theme = _mapper.Map<Theme>(request);
            theme.CreatedAt = DateTime.UtcNow;
            theme.IsActive = true;

            await _themeRepository.AddAsync(theme, cancellationToken);

            return _mapper.Map<ThemeDto>(theme);
        }
    }
}
