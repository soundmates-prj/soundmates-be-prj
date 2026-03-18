using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.SubscriptionPlans.Commands.CreatePlan;
using AccountContentService.Application.Features.SubscriptionPlans.Commands.UpdatePlan;
using AccountContentService.Application.Features.Subscriptions.Commands.CreateSubscription;
using AccountContentService.Domain.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Mappings
{
    public class SubscriptionProfile : Profile
    {
        public SubscriptionProfile()
        {
            CreateMap<SubscriptionPlan, SubscriptionPlanDto>();
            CreateMap<Subscription, SubscriptionDto>()
                .ForMember(dest => dest.PlanName,
                 opt => opt.MapFrom(src => src.Plan.PlanName));
            CreateMap<CreatePlanCommand, SubscriptionPlan>();
            CreateMap<CreateSubscriptionCommand, Subscription>();
            CreateMap<UpdatePlanCommand, SubscriptionPlan>()
                .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
