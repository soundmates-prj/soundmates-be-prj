using AccountContentService.Api.Common;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.BlogComments.Queries.GetComments;
using AccountContentService.Application.Features.SubscriptionPlans.Commands.CreatePlan;
using AccountContentService.Application.Features.SubscriptionPlans.Commands.UpdatePlan;
using AccountContentService.Application.Features.Subscriptions.Queries.GetSubscriptions;
using AutoMapper;

namespace AccountContentService.Api.Mappings
{
    public class SubscriptionMappingProfile : Profile
    {
        public SubscriptionMappingProfile()
        {
            CreateMap<SubscriptionPlanDto, SubscriptionPlanResponse>();
            CreateMap<SubscriptionDto, SubscriptionResponse>();
            CreateMap<SubscriptionPlanRequest, CreatePlanCommand>();
            CreateMap<SubscriptionPlanRequest, UpdatePlanCommand>();
            CreateMap<PaginationNoFilterRequest, GetSubscriptionsHistoryQuery>();

        }
    }
}
