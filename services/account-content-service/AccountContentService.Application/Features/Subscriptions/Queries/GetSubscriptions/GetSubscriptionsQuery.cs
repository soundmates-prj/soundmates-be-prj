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

    /// <summary>
    /// Returns full subscription with plan limits (VoiceModelLimit, TtsMinuteLimit, etc.)
    /// </summary>
    public class GetUserSubscriptionFullQuery : IRequest<SubscriptionDto>
    {
        public Guid UserId { get; set; }
        public GetUserSubscriptionFullQuery(Guid userId)
        {
            UserId = userId;
        }
    }
}
