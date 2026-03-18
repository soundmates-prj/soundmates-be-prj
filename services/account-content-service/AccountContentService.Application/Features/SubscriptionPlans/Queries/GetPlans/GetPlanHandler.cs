using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SubscriptionPlans.Queries.GetPlans
{
    public class GetPlanHandler : IRequestHandler<GetPlanDetailQuery, SubscriptionPlanDto>,
                                  IRequestHandler<GetPlansQuery, List<SubscriptionPlanDto>>
    {
        private readonly ISubscriptionRepository _repository;
        private readonly IMapper _mapper;

        public GetPlanHandler(ISubscriptionRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<SubscriptionPlanDto> Handle(GetPlanDetailQuery request, CancellationToken cancellationToken)
        {
            var plan = await _repository.GetPlanByIdAsync(request.PlanId, cancellationToken);
            if (plan == null)
                throw new KeyNotFoundException("Subscription plan not found.");
            return _mapper.Map<SubscriptionPlanDto>(plan);
        }

        public async Task<List<SubscriptionPlanDto>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
        {
            var plans = await _repository.GetAllPlansAsync(cancellationToken);
            return _mapper.Map<List<SubscriptionPlanDto>>(plans);
        }
    }
}
