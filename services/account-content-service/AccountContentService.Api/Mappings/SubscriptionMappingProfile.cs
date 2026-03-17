using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.SubscriptionPlans.Commands.CreatePlan;
using AccountContentService.Application.Features.SubscriptionPlans.Commands.UpdatePlan;
using AutoMapper;

namespace AccountContentService.Api.Mappings
{
    public class SubscriptionMappingProfile : Profile
    {
        public SubscriptionMappingProfile()
        {
            CreateMap<SubscriptionPlanDto, SubscriptionPlanResponse>();
            CreateMap<SubscriptionPlanRequest, CreatePlanCommand>();
            CreateMap<SubscriptionPlanRequest, UpdatePlanCommand>();
        }
    }
}
