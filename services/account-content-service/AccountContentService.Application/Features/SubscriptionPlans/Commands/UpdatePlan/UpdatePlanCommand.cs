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

        public  string PlanName { get; set; } = string.Empty;

        public  decimal Price { get; set; }

        public  int DurationDays { get; set; }

        public int RequestLimit { get; set; }

        public int VoiceModelLimit { get; set; }

        public int TtsMinuteLimit { get; set; }

        public int PodcastRequestLimit { get; set; }

        public string? Description { get; set; }
    }
}
