using AuthQueryService.Application.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthQueryService.Application.Abstractions.Messaging
{
    public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    {
        Task<ApiResponse<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
    }
}
