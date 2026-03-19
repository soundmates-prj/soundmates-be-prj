using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Subscriptions.Queries.GetSubscriptions
{
    public class GetSubscriptionsQuery : IRequest<PaginationResult<SubscriptionDto>>
    {
        public int Page { get; set; }

        public int PageSize { get; set; }
    }

    public class GetSubscriptionsHistoryQuery : IRequest<PaginationResult<SubscriptionDto>>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; }

        public int PageSize { get; set; }
    }

    public class GetUserSubscriptionQuery : IRequest<SubscriptionDto>
    {
        public Guid UserId { get; set; }
        public GetUserSubscriptionQuery(Guid userId)
        {
            UserId = userId;
        }
    }
}
