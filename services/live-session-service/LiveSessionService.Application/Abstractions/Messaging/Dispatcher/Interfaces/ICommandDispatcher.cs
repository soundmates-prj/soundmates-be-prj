using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Dtos.Response;

namespace LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;

public interface ICommandDispatcher
{
    Task<ApiResponse<object>> Send<TCommand>(TCommand command, CancellationToken ct = default) where TCommand : ICommand;
    Task<ApiResponse<TResponse>> Send<TCommand, TResponse>(TCommand command, CancellationToken ct = default) where TCommand : ICommand<TResponse>;
}
