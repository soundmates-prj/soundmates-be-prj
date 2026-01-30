using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;

namespace LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;

public interface ICommandDispatcher
{
    Task<Result> Send<TCommand>(TCommand command, CancellationToken ct = default) where TCommand : ICommand;
    Task<Result<TResponse>> Send<TCommand, TResponse>(TCommand command, CancellationToken ct = default) where TCommand : ICommand<TResponse>;
}
