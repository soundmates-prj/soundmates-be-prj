using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SubscriptionPlans.Commands.DeletePlan
{
    public class DeletePlanHandler : IRequestHandler<DeletePlanCommand, bool>
    {
        private readonly ISubscriptionRepository _repository;

        public DeletePlanHandler(ISubscriptionRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeletePlanCommand request, CancellationToken cancellationToken)
        {
            var plan = await _repository.GetPlanByIdAsync(request.PlanId, cancellationToken);
            if (plan == null)
            {
                throw new NotFoundException("Plan not found");
            }
            await _repository.DeletePlanAsync(plan);
            return true;
        }
    }
}
