namespace LiveSessionService.Application.Features.LiveSessions.Scheduling;

internal static class ScheduleTimeConverter
{
    public static DateTime ConvertUtcToVietnamLocal(DateTime utcTime)
    {
        var normalizedUtc = utcTime.Kind == DateTimeKind.Utc
            ? utcTime
            : DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);

        var vietnamTimeZone = TryGetTimeZone("SE Asia Standard Time")
            ?? TryGetTimeZone("Asia/Ho_Chi_Minh");

        return vietnamTimeZone is null
            ? normalizedUtc.AddHours(7)
            : TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, vietnamTimeZone);
    }

    public static DateTime ConvertVietnamLocalToUtc(DateTime localTime)
    {
        var vietnamTimeZone = TryGetTimeZone("SE Asia Standard Time")
            ?? TryGetTimeZone("Asia/Ho_Chi_Minh");

        var normalizedLocal = localTime.Kind == DateTimeKind.Unspecified
            ? localTime
            : DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);

        return vietnamTimeZone is null
            ? normalizedLocal.AddHours(-7)
            : TimeZoneInfo.ConvertTimeToUtc(normalizedLocal, vietnamTimeZone);
    }

    private static TimeZoneInfo? TryGetTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }
}
