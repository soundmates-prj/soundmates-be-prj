using System.Threading;

namespace AuthQueryService.Application.Abstractions.Messaging.Dispatcher.Interfaces
{
    public interface IQueryDispatcher
    {
        Task<AuthQueryService.Application.DTOs.Response.ApiResponse<TResponse>> Query<TResponse>(
            IQuery<TResponse> query,
            CancellationToken ct = default);
    }
}