using AuthService.Application.Results;

namespace AuthService.Application.Abstractions.Messaging
{
    /// <summary>
    /// Command handler without response data
    /// Returns Result for application-level error handling
    /// </summary>
    public interface ICommandHandler<in TCommand>
        where TCommand : ICommand
    {
        Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Command handler with typed response
    /// Returns Result{TResponse} for application-level error handling
    /// </summary>
    public interface ICommandHandler<in TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
    }
}
