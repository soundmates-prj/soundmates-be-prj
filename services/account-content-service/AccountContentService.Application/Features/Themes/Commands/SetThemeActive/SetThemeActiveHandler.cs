using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Themes.Commands.SetThemeActive
{
    public class SetThemeActiveHandler
    : IRequestHandler<SetThemeActiveCommand, bool>
    {
        private readonly IThemeRepository _repository;

        public SetThemeActiveHandler(IThemeRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            SetThemeActiveCommand request,
            CancellationToken cancellationToken)
        {
            var theme = await _repository.GetByIdAsync(request.ThemeId, cancellationToken);

            if (theme == null)
            {
                throw new NotFoundException("Theme not found");
            }

            theme.IsActive = !theme.IsActive;
            theme.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(theme, cancellationToken);

            return theme.IsActive;
        }
    }
}
