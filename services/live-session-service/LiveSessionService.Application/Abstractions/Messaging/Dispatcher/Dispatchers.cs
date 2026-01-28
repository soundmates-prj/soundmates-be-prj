using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Dtos.Response;
using Microsoft.Extensions.DependencyInjection;

namespace LiveSessionService.Application.Abstractions.Messaging.Dispatcher;

internal sealed class CommandDispatcher(IServiceProvider sp) : ICommandDispatcher
{
    public async Task<ApiResponse<object>> Send<TCommand>(TCommand command, CancellationToken ct = default) where TCommand : ICommand
    {
        var handler = sp.GetRequiredService<ICommandHandler<TCommand>>();
        return await handler.Handle(command, ct);
    }

    public async Task<ApiResponse<TResponse>> Send<TCommand, TResponse>(TCommand command, CancellationToken ct = default) where TCommand : ICommand<TResponse>
    {
        var handler = sp.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return await handler.Handle(command, ct);
    }
}