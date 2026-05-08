using MediatR;
using AccountContentService.Application.DTOs;

namespace AccountContentService.Application.Features.SystemSettings.Commands.PatchSettingByKey
{
    public class PatchSettingByKeyCommand : IRequest<SystemSettingDto>
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
