using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces
{
    public interface ICommandDispatcher
    {
        Task<ApiResponse<object>> Send<TCommand>(TCommand command, CancellationToken ct = default) where TCommand : ICommand;
        Task<ApiResponse<TResponse>> Send<TCommand, TResponse>(TCommand command, CancellationToken ct = default) where TCommand : ICommand<TResponse>;
    }

}
