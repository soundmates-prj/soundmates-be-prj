using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Themes.Queries.GetUserTheme
{
    public class GetThemeDetailsQuery : IRequest<ThemeDto>
    {
        public Guid ThemeId { get; set; }

        public GetThemeDetailsQuery(Guid themeId)
        {
            ThemeId = themeId; 
        }
    }
}
