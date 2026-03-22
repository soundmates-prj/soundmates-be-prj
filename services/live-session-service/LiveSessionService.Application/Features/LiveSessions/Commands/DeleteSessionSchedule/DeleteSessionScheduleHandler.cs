using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.DeleteSessionSchedule;

public sealed class DeleteSessionScheduleHandler : ICommandHandler<DeleteSessionScheduleCommand>
{
    private readonly ISessionScheduleRepository _scheduleRepository;

    public DeleteSessionScheduleHandler(ISessionScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository;
    }

    public async Task<Result> Handle(DeleteSessionScheduleCommand command, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(command.ScheduleId, cancellationToken);
        if (schedule == null)
        {
            return Result.Failure("Schedule not found", ErrorCode.NotFound);
        }

        await _scheduleRepository.DeleteAsync(schedule, cancellationToken);
        return Result.Success();
    }
}
