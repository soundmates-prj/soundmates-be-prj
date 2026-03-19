using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SubscriptionPlans.Queries.GetPlans
{
    public class GetPlansQuery : IRequest<List<SubscriptionPlanDto>>
    {
        public GetPlansQuery() { }
    }

    public class GetPlanDetailQuery : IRequest<SubscriptionPlanDto>
    {
        public Guid PlanId { get; set; }
        public GetPlanDetailQuery(Guid planId)
        {
            PlanId = planId;
        }
    }
}
