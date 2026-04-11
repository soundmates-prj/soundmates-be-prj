using AccountContentService.Api.Common;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.BlogComments.Queries.GetComments;
using AccountContentService.Application.Features.SubscriptionPlans.Commands.CreatePlan;
using AccountContentService.Application.Features.SubscriptionPlans.Commands.UpdatePlan;
using AccountContentService.Application.Features.Subscriptions.Queries.GetSubscriptions;
using AccountContentService.Domain.Entities;
using AutoMapper;

namespace AccountContentService.Api.Mappings
{
    public class SubscriptionMappingProfile : Profile
    {
        public SubscriptionMappingProfile()
        {
            CreateMap<SubscriptionPlanDto, SubscriptionPlanResponse>();
            CreateMap<SubscriptionDto, SubscriptionResponse>()
                .ForMember(dest => dest.VoiceModelLimit, opt => opt.MapFrom(src => src.VoiceModelLimit))
                .ForMember(dest => dest.TtsMinuteLimit, opt => opt.MapFrom(src => src.TtsMinuteLimit))
                .ForMember(dest => dest.PodcastRequestLimit, opt => opt.MapFrom(src => src.PodcastRequestLimit));

            // Subscription → SubscriptionResponse (direct, for any controller mapping directly from entity)
            CreateMap<Subscription, SubscriptionResponse>()
                .ForMember(dest => dest.VoiceModelLimit, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.VoiceModelLimit : 0))
                .ForMember(dest => dest.TtsMinuteLimit, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.TtsMinuteLimit : 0))
                .ForMember(dest => dest.PodcastRequestLimit, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.PodcastRequestLimit : 0));

            // NOTE: Subscription → SubscriptionDto is defined in Application/Mappings/SubscriptionProfile.cs
            // Do NOT duplicate it here — AutoMapper conflict causes VoiceModelLimit to return 0.

            CreateMap<SubscriptionPlanRequest, CreatePlanCommand>();
            CreateMap<SubscriptionPlanRequest, UpdatePlanCommand>();
            CreateMap<PaginationNoFilterRequest, GetSubscriptionsHistoryQuery>();
        }
    }
}
