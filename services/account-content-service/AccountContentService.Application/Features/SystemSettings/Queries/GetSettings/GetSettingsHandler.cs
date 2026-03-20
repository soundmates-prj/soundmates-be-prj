using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Features.BlogComments.Queries.GetComments;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SystemSettings.Queries.GetSettings
{
    public class GetSettingsHandler
        : IRequestHandler<GetSettingsQuery, PaginationResult<SystemSettingDto>>,
          IRequestHandler<GetSettingDetailByIdQuery, SystemSettingDto>,
          IRequestHandler<GetSettingDetailByKeyQuery, SystemSettingDto>
    {
        private readonly ISystemSettingReposiotry _repository;
        private readonly IMapper _mapper;
        private readonly IEncryptionService _encryptionService;

        public GetSettingsHandler(ISystemSettingReposiotry repository, IMapper mapper, IEncryptionService encryptionService)
        {
            _repository = repository;
            _mapper = mapper;
            _encryptionService = encryptionService;
        }

        public async Task<PaginationResult<SystemSettingDto>> Handle(
             GetSettingsQuery request,
             CancellationToken cancellationToken)
        {
            var result = await _repository.GetAllAsync(request.Page, request.PageSize, cancellationToken);
            var items = result.Items.Select(x =>
            {
                var dto = _mapper.Map<SystemSettingDto>(x);
                dto.Value = _encryptionService.Decrypt(x.Value);
                return dto;
            }).ToList();

            return new PaginationResult<SystemSettingDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<SystemSettingDto> Handle(GetSettingDetailByIdQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (result == null)
            {
                throw new NotFoundException("No setting found");
            }

            var response = _mapper.Map<SystemSettingDto>(result);
            response.Value = _encryptionService.Decrypt(response.Value);
            return response;
        }

        public async Task<SystemSettingDto> Handle(GetSettingDetailByKeyQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetByKeyAsync(request.Key, cancellationToken);
            if (result == null)
            {
                throw new NotFoundException("No setting found");
            }

            var response = _mapper.Map<SystemSettingDto>(result);
            response.Value = _encryptionService.Decrypt(response.Value);
            return response;
        }
    }
}
