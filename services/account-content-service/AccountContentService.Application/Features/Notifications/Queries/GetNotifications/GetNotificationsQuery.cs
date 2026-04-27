using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using MediatR;

namespace AccountContentService.Application.Features.Notifications.Queries.GetNotifications
{
    public class GetNotificationsQuery : IRequest<PaginationResult<NotificationDto>>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetNotReadNotificationsQuery : IRequest<PaginationResult<NotificationDto>>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetReadNotificationsQuery : IRequest<PaginationResult<NotificationDto>>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
