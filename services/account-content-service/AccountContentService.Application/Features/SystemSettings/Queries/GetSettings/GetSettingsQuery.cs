using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SystemSettings.Queries.GetSettings
{
    public class GetSettingsQuery : IRequest<PaginationResult<SystemSettingDto>>
    {
        public int Page {  get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetSettingDetailByIdQuery : IRequest<SystemSettingDto>
    {
        public Guid Id { get; set; }

        public GetSettingDetailByIdQuery(Guid id)
        {
            Id = id; 
        }
    }

    public class GetSettingDetailByKeyQuery : IRequest<SystemSettingDto>
    {
        public string Key { get; set; }

        public GetSettingDetailByKeyQuery(string key)
        {
           Key = key;
        }
    }
}
