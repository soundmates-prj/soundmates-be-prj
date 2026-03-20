using AiService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AiService.Application.Results;
using Microsoft.Extensions.DependencyInjection;

namespace AiService.Application.Abstractions.Messaging.Dispatcher;

internal sealed class CommandDispatcher(IServiceProvider sp) : ICommandDispatcher
{
    public async Task<Result> Send<TCommand>(TCommand command, CancellationToken ct = default) where TCommand : AiService.Application.Abstractions.Messaging.ICommand
    {
        var handler = sp.GetRequiredService<AiService.Application.Abstractions.Messaging.ICommandHandler<TCommand>>();
        return await handler.Handle(command, ct);
    }

    public async Task<Result<TResponse>> Send<TCommand, TResponse>(TCommand command, CancellationToken ct = default)
        where TCommand : AiService.Application.Abstractions.Messaging.ICommand<TResponse>
    {
        var handler = sp.GetRequiredService<AiService.Application.Abstractions.Messaging.ICommandHandler<TCommand, TResponse>>();
        return await handler.Handle(command, ct);
    }
}

internal sealed class QueryDispatcher(IServiceProvider sp) : IQueryDispatcher
{
    public async Task<Result<TResponse>> Send<TQuery, TResponse>(TQuery query, CancellationToken ct = default)
        where TQuery : AiService.Application.Abstractions.Messaging.IQuery<TResponse>
    {
        var handler = sp.GetRequiredService<AiService.Application.Abstractions.Messaging.IQueryHandler<TQuery, TResponse>>();
        return await handler.Handle(query, ct);
    }
}

