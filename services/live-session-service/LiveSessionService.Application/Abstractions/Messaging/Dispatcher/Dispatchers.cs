using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Features.Results;
using Microsoft.Extensions.DependencyInjection;

namespace LiveSessionService.Application.Abstractions.Messaging.Dispatcher;

internal sealed class CommandDispatcher(IServiceProvider sp) : ICommandDispatcher
{
    public async Task<Result> Send<TCommand>(TCommand command, CancellationToken ct = default) where TCommand : ICommand
    {
        var handler = sp.GetRequiredService<ICommandHandler<TCommand>>();
        return await handler.Handle(command, ct);
    }

    public async Task<Result<TResponse>> Send<TCommand, TResponse>(TCommand command, CancellationToken ct = default) where TCommand : ICommand<TResponse>
    {
        var handler = sp.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return await handler.Handle(command, ct);
    }
}