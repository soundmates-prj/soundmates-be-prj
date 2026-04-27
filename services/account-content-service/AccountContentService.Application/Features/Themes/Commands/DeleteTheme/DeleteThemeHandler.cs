using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Themes.Commands.DeleteTheme
{
    public class DeleteThemeHandler : IRequestHandler<DeleteThemeCommand, bool>
    {
        private readonly IThemeRepository _repository;

        public DeleteThemeHandler(IThemeRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            DeleteThemeCommand request,
            CancellationToken cancellationToken)
        {
            var theme = await _repository.GetByIdAsync(request.ThemeId, cancellationToken);

            if (theme == null)
            {
                throw new NotFoundException("theme not found");
            }

            await _repository.DeleteAsync(theme.Id, cancellationToken);

            return true;
        }
    }
}
