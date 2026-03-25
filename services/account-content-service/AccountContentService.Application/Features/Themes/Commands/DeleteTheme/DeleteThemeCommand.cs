using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Themes.Commands.DeleteTheme
{
    public record DeleteThemeCommand : IRequest<bool>
    {
        public Guid ThemeId { get; init; }
        public DeleteThemeCommand(Guid themeId)
        {
            ThemeId = themeId;
        }
    }
}
