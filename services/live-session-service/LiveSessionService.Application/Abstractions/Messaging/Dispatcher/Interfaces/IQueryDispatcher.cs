using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;

namespace LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;

/// <summary>
/// Query dispatcher for handling query operations
/// Similar to ICommandDispatcher but for read operations
/// </summary>
public interface IQueryDispatcher
{
    Task<Result<TResponse>> Send<TQuery, TResponse>(TQuery query, CancellationToken ct = default) 
        where TQuery : IQuery<TResponse>;
}
