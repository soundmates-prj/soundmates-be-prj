using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Application.Features.LiveSessions.Scheduling;

internal static class ScheduleOccurrenceCalculator
{
    public static DateTime? GetNextOccurrenceUtc(SessionSchedule schedule, DateTime nowUtc)
    {
        if (schedule.Status != ScheduleStatus.Scheduled)
            return null;

        return schedule.IsRecurring
            ? GetNextRecurringOccurrenceUtc(schedule, nowUtc)
            : GetOneTimeOccurrenceUtc(schedule, nowUtc);
    }

    public static DateTime? GetNextOccurrenceUtc(IEnumerable<SessionSchedule> schedules, DateTime nowUtc)
    {
        var occurrences = schedules
            .Select(x => GetNextOccurrenceUtc(x, nowUtc))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .OrderBy(x => x)
            .ToList();

        return occurrences.Count > 0 ? occurrences.First() : null;
    }

    private static DateTime? GetOneTimeOccurrenceUtc(SessionSchedule schedule, DateTime nowUtc)
    {
        var localOccurrence = schedule.StartDate.ToDateTime(schedule.StartTime);
        var occurrenceUtc = ScheduleTimeConverter.ConvertVietnamLocalToUtc(localOccurrence);
        return occurrenceUtc > nowUtc ? occurrenceUtc : null;
    }

    private static DateTime? GetNextRecurringOccurrenceUtc(SessionSchedule schedule, DateTime nowUtc)
    {
        if (schedule.DaysOfWeek == DaysOfWeek.None)
            return null;

        var fromDate = DateOnly.FromDateTime(nowUtc.Date) > schedule.StartDate
            ? DateOnly.FromDateTime(nowUtc.Date)
            : schedule.StartDate;

        var toDate = schedule.EndDate ?? fromDate.AddDays(366);
        if (toDate < fromDate)
            return null;

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            if (!IsIncluded(schedule.DaysOfWeek, date.DayOfWeek))
                continue;

            var localCandidate = date.ToDateTime(schedule.StartTime);
            var candidateUtc = ScheduleTimeConverter.ConvertVietnamLocalToUtc(localCandidate);
            if (candidateUtc > nowUtc)
                return candidateUtc;
        }

        return null;
    }

    private static bool IsIncluded(DaysOfWeek mask, DayOfWeek day)
    {
        var flag = day switch
        {
            DayOfWeek.Monday => DaysOfWeek.Monday,
            DayOfWeek.Tuesday => DaysOfWeek.Tuesday,
            DayOfWeek.Wednesday => DaysOfWeek.Wednesday,
            DayOfWeek.Thursday => DaysOfWeek.Thursday,
            DayOfWeek.Friday => DaysOfWeek.Friday,
            DayOfWeek.Saturday => DaysOfWeek.Saturday,
            DayOfWeek.Sunday => DaysOfWeek.Sunday,
            _ => DaysOfWeek.None
        };

        return flag != DaysOfWeek.None && mask.HasFlag(flag);
    }
}
