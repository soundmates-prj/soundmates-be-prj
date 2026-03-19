using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SubscriptionPlans.Commands.UpdatePlan
{
    public class UpdatePlanCommand : IRequest<SubscriptionPlanDto>
    {
        public Guid PlanId { get; set; }

        public  string PlanName { get; set; }

        public  decimal Price { get; set; }

        public  int DurationDays { get; set; }

        public int RequestLimit { get; set; }

        public string? Description { get; set; }
    }
}
