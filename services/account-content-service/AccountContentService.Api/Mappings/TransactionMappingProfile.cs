using AccountContentService.Api.Common;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.PaymentTransactions.Queries.GetTransactions;
using AutoMapper;

namespace AccountContentService.Api.Mappings
{
    public class TransactionMappingProfile : Profile
    {
        public TransactionMappingProfile()
        {
            // UserProfileDto → UserProfileResponse
            CreateMap<UserProfileDto, UserProfileResponse>()
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.ProfileImageUrl));

            CreateMap<TransactionDto, TransactionResponse>();
            CreateMap<PaginationNoFilterRequest, GetTransactionByUserIdQuery>();
            CreateMap<PaginationNoFilterRequest, GetTransactionsQuery>();
        }
    }
}
