using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Themes.Commands.SetThemeActive
{
    public class SetThemeActiveCommand : IRequest<bool>
    {
        public Guid ThemeId { get; set; }
        public SetThemeActiveCommand(Guid themeId)
        {
            ThemeId = themeId;
        }
    }
}
