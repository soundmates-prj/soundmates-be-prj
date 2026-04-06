using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.SearchSchedules;

/// <summary>
/// Search session schedules by keyword and optional filters.
/// Public endpoint — for global search bar.
/// </summary>
public sealed record SearchSchedulesQuery(
    string? Q,
    ScheduleStatus? Status,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 20
) : IQuery<SearchSchedulesResult>;

/// <summary>
/// Concrete result type — avoids generic >> parsing issue.
/// </summary>
public class SearchSchedulesResult
    : LiveSessionService.Application.Features.Results.PagedResult<SessionScheduleResult>
{
    public SearchSchedulesResult(
        List<SessionScheduleResult> items,
        int totalCount,
        int pageNumber,
        int pageSize)
        : base(items, totalCount, pageNumber, pageSize)
    {
    }
}
