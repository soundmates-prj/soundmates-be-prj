using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.SubscriptionPlans.Commands.CreatePlan;
using AccountContentService.Application.Features.SubscriptionPlans.Commands.UpdatePlan;
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
            CreateMap<CreatePlanCommand, SubscriptionPlan>();
            CreateMap<UpdatePlanCommand, SubscriptionPlan>()
                .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
