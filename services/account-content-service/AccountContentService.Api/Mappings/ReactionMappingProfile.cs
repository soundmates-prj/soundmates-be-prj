using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.DTOs;
using AutoMapper;

namespace AccountContentService.Api.Mappings
{
    public class ReactionMappingProfile : Profile
    {
        public ReactionMappingProfile()
        {
            CreateMap<ReactionDto, ReactionResponse>();

        }
    }
}
