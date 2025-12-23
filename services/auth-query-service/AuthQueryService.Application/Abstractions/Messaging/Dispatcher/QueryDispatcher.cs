using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace AuthQueryService.Application.Abstractions.Messaging.Dispatcher
{
    using AuthQueryService.Application.Abstractions.Messaging;
    using AuthQueryService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
    using AuthQueryService.Application.DTOs.Response;

    public sealed class QueryDispatcher : IQueryDispatcher
    {
        private readonly IServiceProvider _sp;

        public QueryDispatcher(IServiceProvider sp) => _sp = sp;

        public Task<ApiResponse<TResponse>> Query<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
        {
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
            var handler = _sp.GetRequiredService(handlerType);

            return ((Task<ApiResponse<TResponse>>)
                handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResponse>, TResponse>.Handle))!
                    .Invoke(handler, new object?[] { query, ct })!);
        }
    }
}
