using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.SubscriptionPlans.Commands.CreatePlan
{
    public class CreatePlanCommand : IRequest<SubscriptionPlanDto>
    {
        public required string PlanName { get; set; }

        public required decimal Price { get; set; }

        public required int DurationDays { get; set; }

        public int RequestLimit { get; set; }

        public string? Description { get; set; }
    }
}
