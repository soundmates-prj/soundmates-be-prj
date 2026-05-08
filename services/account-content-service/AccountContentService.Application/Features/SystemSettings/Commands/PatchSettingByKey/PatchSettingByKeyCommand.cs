using MediatR;
using AccountContentService.Domain.Entities;

namespace AccountContentService.Application.Features.SystemSettings.Commands.PatchSettingByKey
{
    public class PatchSettingByKeyCommand : IRequest<SystemSetting>
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
