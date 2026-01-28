using LiveSessionService.Application.Dtos.Response;

namespace LiveSessionService.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<ApiResponse<object>> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<ApiResponse<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
