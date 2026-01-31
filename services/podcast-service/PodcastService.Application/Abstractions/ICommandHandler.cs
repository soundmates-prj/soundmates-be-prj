using System;
using System.Collections.Generic;
using System.Text;

namespace PodcastService.Application.Abstractions
{
    public interface ICommandHandler<TCommand, TResult>
    {
        Task<TResult> Handle(TCommand command, CancellationToken ct);
    }
}
