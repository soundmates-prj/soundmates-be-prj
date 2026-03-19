using AiService.Application.Results;

namespace AiService.Application.Abstractions.Messaging.Dispatcher.Interfaces;

public interface IQueryDispatcher
{
    Task<Result<TResponse>> Send<TQuery, TResponse>(TQuery query, CancellationToken ct = default)
        where TQuery : AiService.Application.Abstractions.Messaging.IQuery<TResponse>;
}

