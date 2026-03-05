using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Features.Results;
using Microsoft.Extensions.DependencyInjection;

namespace LiveSessionService.Application.Abstractions.Messaging.Dispatcher;

internal sealed class CommandDispatcher(IServiceProvider sp) : ICommandDispatcher
{
    // Send command without response
    public async Task<Result> Send<TCommand>(TCommand command, CancellationToken ct = default) where TCommand : ICommand
    {
        var handler = sp.GetRequiredService<ICommandHandler<TCommand>>();
        return await handler.Handle(command, ct);
    }

    // Send command with response
    public async Task<Result<TResponse>> Send<TCommand, TResponse>(TCommand command, CancellationToken ct = default) where TCommand : ICommand<TResponse>
    {
        var handler = sp.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return await handler.Handle(command, ct);
    }
}

internal sealed class QueryDispatcher(IServiceProvider sp) : IQueryDispatcher
{
    // Send query without response
    public async Task<Result> Send<TQuery>(TQuery query, CancellationToken ct = default) where TQuery : ICommand
    {
        var handler = sp.GetRequiredService<ICommandHandler<TQuery>>();
        return await handler.Handle(query, ct);
    }

    // Send query and get response
    public async Task<Result<TResponse>> Send<TQuery, TResponse>(TQuery query, CancellationToken ct = default) 
        where TQuery : IQuery<TResponse>
    {
        var handler = sp.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return await handler.Handle(query, ct);
    }
}