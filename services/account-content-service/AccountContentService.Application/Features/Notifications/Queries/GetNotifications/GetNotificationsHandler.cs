using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Notifications.Queries.GetNotifications
{
    public class GetNotificationsHandler
    : IRequestHandler<GetNotificationsQuery, PaginationResult<NotificationDto>>
    {
        private readonly INotificationRepository _repository;

        public GetNotificationsHandler(INotificationRepository repository)
        {
            _repository = repository;
        }

        public async Task<PaginationResult<NotificationDto>> Handle(
            GetNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            var items = await _repository.GetByUserIdAsync(
                request.UserId,
                request.Page,
                request.PageSize,
                cancellationToken);

            var total = await _repository.CountByUserIdAsync(
                request.UserId,
                cancellationToken);

            return new PaginationResult<NotificationDto>
            {
                Items = items.Select(x => new NotificationDto
                {
                    Id = x.Id,
                    Type = x.Type,
                    ReferenceId = x.ReferenceId,
                    Message = x.Message,
                    IsRead = x.IsRead,
                    CreatedAt = x.CreatedAt
                }),
                TotalCount = total,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
