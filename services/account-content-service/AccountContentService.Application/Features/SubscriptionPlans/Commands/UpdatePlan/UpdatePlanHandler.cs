using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SubscriptionPlans.Commands.UpdatePlan
{
    public class UpdatePlanHandler : IRequestHandler<UpdatePlanCommand, SubscriptionPlanDto>
    {
        private readonly ISubscriptionRepository _repository;
        private readonly IMapper _mapper;
        public UpdatePlanHandler(ISubscriptionRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
        public async Task<SubscriptionPlanDto> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
        {
            var existingPlan = await _repository.GetPlanByIdAsync(request.PlanId, cancellationToken);
            if (existingPlan == null)
            {
                throw new NotFoundException($"Subscription plan with ID {request.PlanId} not found.");
            }

            _mapper.Map(request, existingPlan);
            existingPlan.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdatePlanAsync(existingPlan);
            return _mapper.Map<SubscriptionPlanDto>(existingPlan);
        }
    }
}
