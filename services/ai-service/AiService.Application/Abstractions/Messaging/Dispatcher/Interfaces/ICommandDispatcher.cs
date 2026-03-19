using AiService.Application.Results;

namespace AiService.Application.Abstractions.Messaging.Dispatcher.Interfaces;

public interface ICommandDispatcher
{
    Task<Result> Send<TCommand>(TCommand command, CancellationToken ct = default) where TCommand : AiService.Application.Abstractions.Messaging.ICommand;

    Task<Result<TResponse>> Send<TCommand, TResponse>(TCommand command, CancellationToken ct = default)
        where TCommand : AiService.Application.Abstractions.Messaging.ICommand<TResponse>;
}

