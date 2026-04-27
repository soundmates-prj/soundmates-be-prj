using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.BlogPostReactions.Commands.CreateReaction;
using AccountContentService.Application.Features.BlogPostReactions.Commands.UpdateReaction;
using AutoMapper;

namespace AccountContentService.Api.Mappings
{
    public class ReactionMappingProfile : Profile
    {
        public ReactionMappingProfile()
        {
            CreateMap<ReactionDto, ReactionResponse>();
            CreateMap<ReactionRequest, CreateReactionCommand>();
            CreateMap<ReactionRequest, UpdateReactionCommand>();

        }
    }
}
