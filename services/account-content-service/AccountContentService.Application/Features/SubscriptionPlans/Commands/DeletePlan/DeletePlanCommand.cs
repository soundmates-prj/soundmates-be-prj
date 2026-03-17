using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SubscriptionPlans.Commands.DeletePlan
{
    public class DeletePlanCommand : IRequest<bool>
    {
        public  Guid PlanId { get; set; }
        public DeletePlanCommand(Guid planId)
        {
            PlanId = planId;
        }
    }
}
