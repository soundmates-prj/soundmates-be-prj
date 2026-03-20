using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SystemSettings.Commands.DeleteSetting
{
    public class DeleteSettingCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public DeleteSettingCommand(Guid id)
        {
            Id = id;
        }
    }
}
