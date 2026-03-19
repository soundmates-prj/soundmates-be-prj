using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Subscriptions.Queries.GetSubscriptions
{
    public class GetSubscriptionsHandler
        : IRequestHandler<GetSubscriptionsQuery, PaginationResult<SubscriptionDto>>,
          IRequestHandler<GetSubscriptionsHistoryQuery, PaginationResult<SubscriptionDto>>,
          IRequestHandler<GetUserSubscriptionQuery, SubscriptionDto>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IMapper _mapper;

        public GetSubscriptionsHandler(ISubscriptionRepository subscriptionRepository, IMapper mapper)
        {
            _subscriptionRepository = subscriptionRepository;
            _mapper = mapper;
        }

        public async Task<PaginationResult<SubscriptionDto>> Handle(GetSubscriptionsQuery request, CancellationToken cancellationToken)
        {
            var result = await _subscriptionRepository.GetSubscriptionsAsync(request.Page, request.PageSize, cancellationToken);
            var items = _mapper.Map<IEnumerable<SubscriptionDto>>(result.Items).ToList();

            return new PaginationResult<SubscriptionDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };

        }
        public async Task<PaginationResult<SubscriptionDto>> Handle(GetSubscriptionsHistoryQuery request, CancellationToken cancellationToken)
        {
            var result = await _subscriptionRepository.GetSubscriptionsHistoryAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
            var items = _mapper.Map<IEnumerable<SubscriptionDto>>(result.Items).ToList();

            return new PaginationResult<SubscriptionDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
        public async Task<SubscriptionDto> Handle(GetUserSubscriptionQuery request, CancellationToken cancellationToken)
        {
            var result = await _subscriptionRepository.GetSubscriptionByUserIdAsync(request.UserId, cancellationToken);
            var subscriptionDto = _mapper.Map<SubscriptionDto>(result);
            return subscriptionDto;
        }
    }
}
