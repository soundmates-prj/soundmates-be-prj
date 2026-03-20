using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SystemSettings.Commands.DeleteSetting
{
    public class DeleteSettingHandler : IRequestHandler<DeleteSettingCommand, bool>
    {
        private readonly ISystemSettingReposiotry _repository;

        public DeleteSettingHandler(ISystemSettingReposiotry repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeleteSettingCommand request, CancellationToken cancellationToken)
        {
            var setting = await _repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Setting not found");
            await _repository.DeleteAsync(setting);
            return true;
        }
    }
}
