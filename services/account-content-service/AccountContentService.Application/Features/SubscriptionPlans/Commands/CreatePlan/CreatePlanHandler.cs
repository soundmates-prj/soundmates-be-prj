using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SubscriptionPlans.Commands.CreatePlan
{
    public class CreatePlanHandler : IRequestHandler<CreatePlanCommand, SubscriptionPlanDto>
    {
        private readonly ISubscriptionRepository _repository;
        private readonly IMapper _mapper;

        public CreatePlanHandler(ISubscriptionRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<SubscriptionPlanDto> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
        {
            var planEntity = _mapper.Map<SubscriptionPlan>(request);
            planEntity.CreatedAt = DateTime.UtcNow;
            planEntity.IsActive = true; // New plans are active by default

            await _repository.AddPlanAsync(planEntity);
            return _mapper.Map<SubscriptionPlanDto>(planEntity);
        }

    }
}
