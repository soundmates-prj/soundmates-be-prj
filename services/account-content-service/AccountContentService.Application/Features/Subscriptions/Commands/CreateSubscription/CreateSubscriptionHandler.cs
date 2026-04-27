using AccountContentService.Application.Abstractions;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AccountContentService.Application.Features.Subscriptions.Commands.CreateSubscription
{
    public class CreateSubscriptionHandler : IRequestHandler<CreateSubscriptionCommand, SubscriptionDto>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IMapper _mapper;

        public CreateSubscriptionHandler(ISubscriptionRepository subscriptionRepository, IMapper mapper)
        {
            _subscriptionRepository = subscriptionRepository;
            _mapper = mapper;
        }

        public async Task<SubscriptionDto> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var subscription = _mapper.Map<Subscription>(request);
            subscription.Status = SubscriptionStatus.Pending.ToString();

            await _subscriptionRepository.AddAsync(subscription);

            return _mapper.Map<SubscriptionDto>(subscription);
        }
    }
}
